using LightTube.Database.Models;
using MongoDB.Driver;

namespace LightTube.Database;

public class Oauth2Manager
{
	public IMongoCollection<DatabaseOauthToken> OauthTokensCollection { get; }

	public Oauth2Manager(IMongoCollection<DatabaseOauthToken> oauthTokensCollection)
	{
		OauthTokensCollection = oauthTokensCollection;
	}

	public async Task<string> CreateOauthToken(string loginToken, string clientId, string[] scopes)
	{
		DatabaseUser user = (await DatabaseManager.Users.GetUserFromToken(loginToken))!;
		string grantCode = Utils.GenerateToken(32);
		await OauthTokensCollection.InsertOneAsync(new DatabaseOauthToken
		{
			UserId = user.UserID,
			OauthGrantCode = grantCode,
			ClientId = clientId,
			RefreshToken = Utils.GenerateToken(512),
			CurrentAuthToken = null,
			CurrentTokenExpirationDate = DateTimeOffset.UnixEpoch,
			Scopes = scopes
		});
		return grantCode;
	}

	public async Task<DatabaseOauthToken?> RefreshToken(string refreshToken, string clientId)
	{
		await Task.Delay(1000);
		// returns null sometimes :sob:
		DatabaseOauthToken? token =
			(await OauthTokensCollection
				.FindAsync(x => (x.OauthGrantCode == refreshToken /* && x.ClientId == clientId*/) || x.RefreshToken == refreshToken))
			.FirstOrDefault();
		if (token is null) return null;
		token.CurrentAuthToken = Utils.GenerateToken(256);
		token.CurrentTokenExpirationDate = DateTimeOffset.Now.AddHours(1);
		token.OauthGrantCode = null;
		await OauthTokensCollection.FindOneAndReplaceAsync(x => x.OauthGrantCode == refreshToken, token);
		return token;
	}

	public async Task<DatabaseUser?> GetUserFromHeader(string authHeader)
	{
		string[] parts = authHeader.Split(" ");
		string type = parts[0].ToLower();
		string token = parts[1].ToLower();

		IAsyncCursor<DatabaseOauthToken> cursor = await OauthTokensCollection.FindAsync(x => x.CurrentAuthToken == token);
		DatabaseOauthToken login = cursor.FirstOrDefault();

		if (login is null)
			return null;

		return await DatabaseManager.Users.GetUserFromId(login.UserId);
	}
}