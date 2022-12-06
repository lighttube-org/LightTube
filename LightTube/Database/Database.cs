using LightTube.Database.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LightTube.Database;

public class Database
{
	private static IMongoCollection<DatabaseUser> _userCollection;

	public static void Init(string connstr)
	{
		MongoClient client = new(connstr);
		IMongoDatabase database = client.GetDatabase("lighttube");
		_userCollection = database.GetCollection<DatabaseUser>("users");
		
		RunMigrations();
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
}