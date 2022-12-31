using LightTube.Database;
using LightTube.Database.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LightTube.Chores;

public class MigrateDataChore : IChore
{
	public string Id => "MigrateData";

	public async Task<string> RunChore(Action<string> updateStatus, Guid id)
	{
		int migratedUserCount = 0;
		int migratedLoginCount = 0;

		updateStatus("Migrating users...");
		IAsyncCursor<DatabaseUser> unmigratedUsers = await DatabaseManager.UserCollection.FindAsync(x => x.SubscribedChannels != null || (x.Email != null && x.UserID == null) || x.LTChannelID == null);
		while (await unmigratedUsers.MoveNextAsync())
		{
			foreach (DatabaseUser user in unmigratedUsers.Current)
			{
				user.Migrate();
				await DatabaseManager.UserCollection.FindOneAndReplaceAsync(x => x.PasswordHash == user.PasswordHash, // surely the password hash wouldnt change  
					user);
				migratedUserCount++;
			}
		}

		updateStatus("Migrating tokens...");
		IMongoCollection<BsonDocument> tokensCollection = DatabaseManager.Database.GetCollection<BsonDocument>("tokens");
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
				migratedLoginCount++;
			}
		} 

		return $"migrated\n- {migratedUserCount} users\n- {migratedLoginCount} tokens";
	}
}