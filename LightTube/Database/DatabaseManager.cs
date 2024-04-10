using LightTube.Chores;
using LightTube.Database.Models;
using MongoDB.Driver;

namespace LightTube.Database;

public static class DatabaseManager
{
    public static IMongoDatabase Database { get; private set; }
    public static IMongoCollection<DatabaseUser> UserCollection { get; private set; }
    public static IMongoCollection<DatabaseLogin> TokensCollection { get; private set; }
    public static IMongoCollection<DatabaseOauthToken> Oauth2TokensCollection { get; private set; }
    public static IMongoCollection<DatabaseVideo> VideoCacheCollection { get; private set; }
    public static IMongoCollection<DatabasePlaylist> PlaylistCollection { get; private set; }
    public static IMongoCollection<DatabaseChannel> ChannelCacheCollection { get; private set; }
    public static UserManager Users { get; private set; }
    public static CacheManager Cache { get; private set; }
    public static Oauth2Manager Oauth2 { get; private set; }
    public static PlaylistManager Playlists { get; private set; }

    public static void Init(string connstr)
    {
        MongoClient client = new(connstr);
        Database = client.GetDatabase(Configuration.Database);
        UserCollection = Database.GetCollection<DatabaseUser>("users");
        TokensCollection = Database.GetCollection<DatabaseLogin>("tokens");
        VideoCacheCollection = Database.GetCollection<DatabaseVideo>("videoCache");
        PlaylistCollection = Database.GetCollection<DatabasePlaylist>("playlists");
        ChannelCacheCollection = Database.GetCollection<DatabaseChannel>("channelCache");
        Oauth2TokensCollection = Database.GetCollection<DatabaseOauthToken>("oauth2Tokens");

        Users = new UserManager(UserCollection, TokensCollection, PlaylistCollection, Oauth2TokensCollection);
        Cache = new CacheManager(ChannelCacheCollection, VideoCacheCollection);
        Oauth2 = new Oauth2Manager(Oauth2TokensCollection);
        Playlists = new PlaylistManager(PlaylistCollection, VideoCacheCollection);

        ChoreManager.QueueChore("DatabaseCleanup");
    }
}