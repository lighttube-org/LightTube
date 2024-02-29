using LightTube.Database.Models;
using Microsoft.Extensions.Primitives;
using MongoDB.Driver;

namespace LightTube.Database;

public class Oauth2Manager(IMongoCollection<DatabaseOauthToken> oauthTokensCollection)
{
    public IMongoCollection<DatabaseOauthToken> OauthTokensCollection { get; } = oauthTokensCollection;

    public async Task<string> CreateOauthToken(string loginToken, string clientId, string[] scopes)
	{
		DatabaseUser user = (await DatabaseManager.Users.GetUserFromToken(loginToken))!;
		string refreshToken = Utils.GenerateToken(512);
		await OauthTokensCollection.InsertOneAsync(new DatabaseOauthToken
		{
			UserId = user.UserID,
			ClientId = clientId,
			RefreshToken = refreshToken,
			CurrentAuthToken = null,
			CurrentTokenExpirationDate = DateTimeOffset.UnixEpoch,
			Scopes = scopes
		});
		return refreshToken;
	}

	public async Task<DatabaseOauthToken?> RefreshToken(string refreshToken, string clientId)
	{
		await Task.Delay(1000);
		// returns null sometimes :sob:
		DatabaseOauthToken? token =
			(await OauthTokensCollection
				.FindAsync(x => x.RefreshToken == refreshToken))
			.FirstOrDefault();
		if (token is null) return null;
		token.CurrentAuthToken = Utils.GenerateToken(256);
		token.CurrentTokenExpirationDate = DateTimeOffset.Now.AddHours(1);
		await OauthTokensCollection.FindOneAndReplaceAsync(x => x.RefreshToken == refreshToken, token);
		return token;
	}

	public async Task<DatabaseUser?> GetUserFromHeader(string authHeader)
	{
		string[] parts = authHeader.Split(" ");
		string type = parts[0].ToLower();
		string token = parts[1];

		if (type != "bearer") return null;

		IAsyncCursor<DatabaseOauthToken> cursor =
			await OauthTokensCollection.FindAsync(x => x.CurrentAuthToken == token);
		DatabaseOauthToken login = cursor.FirstOrDefault();

		if (login is null)
			return null;

		return await DatabaseManager.Users.GetUserFromId(login.UserId);
	}

	public async Task<DatabaseOauthToken?> GetLoginFromHttpContext(HttpContext context)
	{
		if (!context.Request.Headers.TryGetValue("authorization", out StringValues authHeaders))
			return null;

		string[] parts = authHeaders.First().Split(" ");
		string type = parts[0].ToLower();
		string token = parts[1];

		if (type != "bearer") return null;

		IAsyncCursor<DatabaseOauthToken> cursor =
			await OauthTokensCollection.FindAsync(x => x.CurrentAuthToken == token);
		DatabaseOauthToken login = cursor.FirstOrDefault();

		return login;
	}

	public async Task<DatabaseUser?> GetUserFromHttpRequest(HttpRequest request)
	{
		if (!request.Headers.TryGetValue("authorization", out StringValues authHeaders))
			return null;

		string authHeader = authHeaders.First();
		return await DatabaseManager.Oauth2.GetUserFromHeader(authHeader);
	}
}