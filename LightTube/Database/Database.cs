using System.Diagnostics;
using LightTube.Database.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LightTube.Database;

public static class Database
{
	private static IMongoDatabase _database;
	private static IMongoCollection<DatabaseUser> _userCollection;
	private static IMongoCollection<DatabaseLogin> _tokensCollection;
	private static IMongoCollection<DatabaseVideo> _videoCacheCollection;
	private static IMongoCollection<DatabasePlaylist> _playlistCollection;
	private static IMongoCollection<DatabaseChannel> _channelCacheCollection;

	public static void Init(string connstr)
	{
		MongoClient client = new(connstr);
		_database = client.GetDatabase(Configuration.GetVariable("LIGHTTUBE_MONGODB_DATABASE", "lighttube"));
		_userCollection = _database.GetCollection<DatabaseUser>("users");
		_tokensCollection = _database.GetCollection<DatabaseLogin>("tokens");
		_videoCacheCollection = _database.GetCollection<DatabaseVideo>("videoCache");
		_playlistCollection = _database.GetCollection<DatabasePlaylist>("playlists");
		_channelCacheCollection = _database.GetCollection<DatabaseChannel>("channelCache");

		RunMigrations();
		//ClearJunkData();
	}

	public static async void RunMigrations()
	{
		IAsyncCursor<DatabaseUser> unmigratedUsers = await _userCollection.FindAsync(x => x.SubscribedChannels != null || (x.Email != null && x.UserID == null) || x.LTChannelID == null);
		while (await unmigratedUsers.MoveNextAsync())
		{
			foreach (DatabaseUser user in unmigratedUsers.Current)
			{
				Console.WriteLine("Migrating user: " + (user.UserID ?? "{!}" + user.Email));
				user.Migrate();
				DatabaseUser replaceAsync =
					await _userCollection.FindOneAndReplaceAsync(x => x.PasswordHash == user.PasswordHash, // surely the password hash wouldnt change  
						user);
				Console.WriteLine("Migrated user: " + (replaceAsync.UserID ?? "{!}" + replaceAsync.Email));
			}
		}

		Console.WriteLine("Migrating tokens...");
		IMongoCollection<BsonDocument> tokensCollection = _database.GetCollection<BsonDocument>("tokens");
		IAsyncCursor<BsonDocument> unmigratedTokens = await tokensCollection.FindAsync(x => true);
		while (await unmigratedTokens.MoveNextAsync())
		{
			foreach (BsonDocument token in unmigratedTokens.Current.Where(x => x.Contains("Identifier")))
			{
				token["_id"] = token["Identifier"];
				token["UserID"] = token["Email"];
				token.Remove("Identifier");
				token.Remove("Email");
				await tokensCollection.DeleteOneAsync(x => x["Token"] == token["Token"]);
				await tokensCollection.InsertOneAsync(token);
			}
		} 
		Console.WriteLine("Migrated tokens");
	}

	public static async void ClearJunkData()
	{
		Stopwatch sp = Stopwatch.StartNew();
		List<string> channels = new();
		List<string> videos = new();
		List<string> users = new();
		int deletedChannels = 0;
		int deletedVideos = 0;
		int deletedPlaylists = 0;
		int deletedTokens = 0;

		IAsyncCursor<DatabaseUser> allUsers = await _userCollection.FindAsync(x => true);
		while (await allUsers.MoveNextAsync())
			foreach (DatabaseUser user in allUsers.Current)
			{
				if (users.Contains(user.UserID))
					Console.WriteLine("Duplicate UserID: " + user.UserID);
				else
					users.Add(user.UserID);
				foreach (string channel in user.Subscriptions?.Keys.ToArray() ?? user.SubscribedChannels)
					if (!channels.Contains(channel))
						channels.Add(channel);
			}

		IAsyncCursor<DatabasePlaylist> playlists = await _playlistCollection.FindAsync(x => true);
		while (await playlists.MoveNextAsync())
			foreach (DatabasePlaylist playlist in playlists.Current)
			{
				if (!users.Contains(playlist.Author))
				{
					Console.WriteLine($"Playlist {playlist.Name} does not belong to anyone, deleting it...");
					deletedPlaylists++;
					await _playlistCollection.DeleteOneAsync(x => x.Id == playlist.Id);
					continue;
				}
				foreach (string videoId in playlist.VideoIds)
					if (!videos.Contains(videoId))
						videos.Add(videoId);
			}

		IMongoQueryable<DatabaseChannel> cachedChannels = _channelCacheCollection.AsQueryable();
		foreach (DatabaseChannel channel in cachedChannels)
		{
			if (!channels.Contains(channel.ChannelId))
			{
				await _channelCacheCollection.DeleteOneAsync(x => x.ChannelId == channel.ChannelId);
				deletedChannels++;
				if (sp.ElapsedMilliseconds % 5000 == 0)
					Console.WriteLine($"Deleted {deletedChannels} channels from the cache, and still going on...");
			}
		}
		Console.WriteLine($"Deleted {deletedChannels} channels from the cache");

		IMongoQueryable<DatabaseVideo> cachedVideos = _videoCacheCollection.AsQueryable();
		foreach (DatabaseVideo video in cachedVideos)
		{
			if (!videos.Contains(video.Id))
			{
				await _videoCacheCollection.DeleteOneAsync(x => x.Id == video.Id);
				deletedVideos++;
				if (sp.ElapsedMilliseconds % 5000 == 0)
					Console.WriteLine($"Deleted {deletedVideos} videos from the cache, and still going on...");
			}
		}
		Console.WriteLine($"Deleted {deletedVideos} videos from the cache");

		IMongoQueryable<DatabaseLogin> tokens = _tokensCollection.AsQueryable();
		foreach (DatabaseLogin login in tokens)
		{
			//todo: delete tokens older than x days
			if (!users.Contains(login.UserID)) 
				await _tokensCollection.DeleteOneAsync(x => x.Id == login.Id);
		}
		Console.WriteLine($"Deleted {deletedVideos} videos from the cache");
		
		sp.Stop();
		Console.WriteLine($"Chore ClearJunkData done in {sp.Elapsed}, deleted\n- {deletedChannels} channels\n- {deletedPlaylists} playlists\n- {deletedVideos} videos");
	}
}