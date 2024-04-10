using System.Data;
using System.Text;
using LightTube.Database.Models;
using MongoDB.Driver;

namespace LightTube.Database;

public class UserManager(IMongoCollection<DatabaseUser> userCollection,
    IMongoCollection<DatabaseLogin> tokensCollection,
    IMongoCollection<DatabasePlaylist> playlistCollection,
    IMongoCollection<DatabaseOauthToken> oauth2TokensCollection)
{
    public IMongoCollection<DatabaseUser> UserCollection { get; } = userCollection;
    public IMongoCollection<DatabaseLogin> TokensCollection { get; } = tokensCollection;
    public IMongoCollection<DatabaseOauthToken> Oauth2TokensCollection { get; } = oauth2TokensCollection;
    public IMongoCollection<DatabasePlaylist> PlaylistCollection { get; } = playlistCollection;

    public async Task<DatabaseUser?> GetUserFromUsernamePassword(string userId, string password)
    {
        IAsyncCursor<DatabaseUser> users = await UserCollection.FindAsync(x => x.UserID == userId);
        if (!await users.AnyAsync())
            throw new UnauthorizedAccessException("Invalid credentials");
        DatabaseUser user = (await UserCollection.FindAsync(x => x.UserID == userId)).First();
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");
        return user;
    }

    public async Task<DatabaseUser?> GetUserFromToken(string token)
    {
        if (token.StartsWith("Bearer "))
        {
            token = token.Split(" ")[1];
            IAsyncCursor<DatabaseOauthToken> loginCursor =
                await Oauth2TokensCollection.FindAsync(x => x.CurrentAuthToken == token);
            DatabaseOauthToken login = await loginCursor.FirstOrDefaultAsync();
            if (login is null) return null;

            IAsyncCursor<DatabaseUser> userCursor = await UserCollection.FindAsync(x => x.UserID == login.UserId);
            return await userCursor.FirstOrDefaultAsync();
        }
        else
        {
            IAsyncCursor<DatabaseLogin> loginCursor = await TokensCollection.FindAsync(x => x.Token == token);
            DatabaseLogin login = await loginCursor.FirstOrDefaultAsync();

            if (login is null) return null;

            await TokensCollection.FindOneAndUpdateAsync(
                Builders<DatabaseLogin>.Filter.Eq(x => x.Id, login.Id),
                Builders<DatabaseLogin>.Update.Set(x => x.LastSeen, DateTimeOffset.UtcNow));

            IAsyncCursor<DatabaseUser> userCursor = await UserCollection.FindAsync(x => x.UserID == login.UserID);
            return await userCursor.FirstOrDefaultAsync();
        }
    }

    public async Task<DatabaseUser?> GetUserFromId(string id)
    {
        IAsyncCursor<DatabaseUser> userCursor = await UserCollection.FindAsync(x => x.UserID == id);
        return await userCursor.FirstOrDefaultAsync();
    }

    public async Task<DatabaseUser?> GetUserFromLTId(string id)
    {
        IAsyncCursor<DatabaseUser> userCursor = await UserCollection.FindAsync(x => x.LTChannelID == id);
        return await userCursor.FirstOrDefaultAsync();
    }

    public async Task<DatabaseLogin> CreateToken(string userId, string password, string userAgent,
        IEnumerable<string> scopes)
    {
        IAsyncCursor<DatabaseUser> users = await UserCollection.FindAsync(x => x.UserID == userId);
        if (!await users.AnyAsync())
            throw new UnauthorizedAccessException("Invalid credentials");
        DatabaseUser user = (await UserCollection.FindAsync(x => x.UserID == userId)).First();
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        DatabaseLogin login = new()
        {
            Id = Guid.NewGuid().ToString(),
            UserID = userId,
            Token = Utils.GenerateToken(256),
            UserAgent = userAgent,
            Scopes = scopes.ToArray(),
            Created = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow
        };
        await TokensCollection.InsertOneAsync(login);
        return login;
    }

    public async Task UpdateLastAccess(string token, DateTimeOffset offset)
    {
        DatabaseLogin login = (await TokensCollection.FindAsync(x => x.Token == token)).First();
        login.LastSeen = offset;
        await TokensCollection.ReplaceOneAsync(x => x.Token == token, login);
    }

    public async Task RemoveToken(string token)
    {
        await TokensCollection.FindOneAndDeleteAsync(t => t.Token == token);
    }

    public async Task RemoveToken(string sourceToken, string id)
    {
        DatabaseLogin login = (await TokensCollection.FindAsync(x => x.Token == sourceToken)).First();
        DatabaseLogin deletedLogin = (await TokensCollection.FindAsync(x => x.Id == id)).First();

        if (login.UserID == deletedLogin.UserID)
            await TokensCollection.FindOneAndDeleteAsync(t => t.Id == id);
        else
            throw new UnauthorizedAccessException(
                "Logged in user does not match the token that is supposed to be deleted");
    }

    public async Task<List<DatabaseLogin>> GetAllUserTokens(string token)
    {
        string userId = (await TokensCollection.FindAsync(x => x.Token == token)).First().UserID;
        return await (await TokensCollection.FindAsync(u => u.UserID == userId)).ToListAsync();
    }

    public async Task<(string channelId, SubscriptionType subscriptionType)> UpdateSubscription(string token,
        string channelId, SubscriptionType type)
    {
        DatabaseUser? user = await GetUserFromToken(token) ?? throw new UnauthorizedAccessException();

        // TODO: update the channel cache

        if (user.Subscriptions.ContainsKey(channelId))
            if (type == SubscriptionType.NONE)
                user.Subscriptions.Remove(channelId);
            else
                user.Subscriptions[channelId] = type;
        else if (type != SubscriptionType.NONE)
            user.Subscriptions.Add(channelId, type);

        await UserCollection.ReplaceOneAsync(x => x.UserID == user.UserID, user);

        return user.Subscriptions.TryGetValue(channelId, out SubscriptionType value) ? (channelId, value)
            : (channelId, SubscriptionType.NONE);
    }

    public async Task DeleteUser(string userId, string password)
    {
        IAsyncCursor<DatabaseUser> users = await UserCollection.FindAsync(x => x.UserID == userId);
        if (!await users.AnyAsync())
            throw new KeyNotFoundException("Invalid credentials");
        DatabaseUser user = (await UserCollection.FindAsync(x => x.UserID == userId)).First();
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        await UserCollection.DeleteOneAsync(x => x.UserID == userId);
        await TokensCollection.DeleteManyAsync(x => x.UserID == userId);
        // TODO: delete user playlists
        //foreach (DatabasePlaylist pl in await DatabaseManager.Playlists.GetUserPlaylists(userId))
        //	await DatabaseManager.Playlists.DeletePlaylist(pl.Id);
    }

    public async Task CreateUser(string userId, string password)
    {
        IAsyncCursor<DatabaseUser> users = await UserCollection.FindAsync(x => x.UserID == userId);
        if (await users.AnyAsync())
            throw new DuplicateNameException("A user with that User ID already exists");

        DatabaseUser user = DatabaseUser.CreateUser(userId, password);
        await UserCollection.InsertOneAsync(user);
    }
}