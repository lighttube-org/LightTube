using System.Diagnostics;
using LightTube.Database.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LightTube.Database;

public static class Database
{
	private static IMongoCollection<DatabaseUser> _userCollection;
	private static IMongoCollection<DatabaseVideo> _videoCacheCollection;
	private static IMongoCollection<DatabasePlaylist> _playlistCollection;
	private static IMongoCollection<DatabaseChannel> _channelCacheCollection;

	public static void Init(string connstr)
	{
		MongoClient client = new(connstr);
		IMongoDatabase database = client.GetDatabase(Configuration.GetVariable("LIGHTTUBE_MONGODB_DATABASE", "lighttube"));
		_userCollection = database.GetCollection<DatabaseUser>("users");
		_videoCacheCollection = database.GetCollection<DatabaseVideo>("videoCache");
		_playlistCollection = database.GetCollection<DatabasePlaylist>("playlists");
		_channelCacheCollection = database.GetCollection<DatabaseChannel>("channelCache");

		RunMigrations();
		ClearJunkData();
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
		
		sp.Stop();
		Console.WriteLine($"Chore ClearJunkData done in {sp.Elapsed}, deleted\n- {deletedChannels} channels\n- {deletedPlaylists} playlists\n- {deletedVideos} videos");
	}
}