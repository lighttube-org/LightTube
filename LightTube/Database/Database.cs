using LightTube.Database.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LightTube.Database;

public class Database
{
	private static IMongoCollection<DatabaseUser> _userCollection;
	private static IMongoCollection<DatabaseChannel> _channelCacheCollection;

	public static void Init(string connstr)
	{
		MongoClient client = new(connstr);
		IMongoDatabase database = client.GetDatabase("lighttube");
		_userCollection = database.GetCollection<DatabaseUser>("users");
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
				Console.WriteLine("Migrating: " + (user.UserID ?? "{!}" + user.Email));
				user.Migrate();
				DatabaseUser replaceAsync =
					await _userCollection.FindOneAndReplaceAsync(x => x.PasswordHash == user.PasswordHash, // surely the password hash wouldnt change  
						user);
				Console.WriteLine("Migrated: " + (replaceAsync.UserID ?? "{!}" + replaceAsync.Email));
			}
		}
	}

	public static async void ClearJunkData()
	{
		List<string> channels = new();	
		IAsyncCursor<DatabaseUser> allUsers = await _userCollection.FindAsync(x => true);
		while (await allUsers.MoveNextAsync())
			foreach (DatabaseUser user in allUsers.Current)
				foreach (string channel in user.Subscriptions.Keys)
					if (!channels.Contains(channel))
						channels.Add(channel);

		IMongoQueryable<DatabaseChannel> cachedChannels = _channelCacheCollection.AsQueryable();
		int deletedChannels = 0;
		foreach (DatabaseChannel channel in cachedChannels)
		{
			if (!channels.Contains(channel.ChannelId))
			{
				await _channelCacheCollection.DeleteOneAsync(x => x.ChannelId == channel.ChannelId);
				deletedChannels++;
				if (deletedChannels % 5000 == 0)
					Console.WriteLine($"Deleted {deletedChannels} channels from the cache, and still going on...");
			}
		}
		Console.WriteLine($"Deleted {deletedChannels} channels from the cache");
	}
}