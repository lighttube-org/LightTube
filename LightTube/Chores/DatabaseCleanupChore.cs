using LightTube.Database;
using LightTube.Database.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace LightTube.Chores;

public class DatabaseCleanupChore : IChore
{
    public string Id => "DatabaseCleanup";

    public async Task<string> RunChore(Action<string> updateStatus, Guid id)
    {
        List<string> channels = [];
        List<string> videos = [];
        List<string> users = [];
        int deletedChannels = 0;
        int deletedVideos = 0;
        int deletedPlaylists = 0;
        int deletedTokens = 0;

        IAsyncCursor<DatabaseUser> allUsers = await DatabaseManager.UserCollection.FindAsync(x => true);
        while (await allUsers.MoveNextAsync())
            foreach (DatabaseUser user in allUsers.Current)
            {
                if (users.Contains(user.UserID))
                    updateStatus("Duplicate UserID: " + user.UserID);
                else
                    users.Add(user.UserID);
                foreach (string channel in user.Subscriptions?.Keys.ToArray() ?? user.SubscribedChannels)
                    if (!channels.Contains(channel))
                        channels.Add(channel);
            }

        IAsyncCursor<DatabasePlaylist> playlists = await DatabaseManager.PlaylistCollection.FindAsync(x => true);
        while (await playlists.MoveNextAsync())
            foreach (DatabasePlaylist playlist in playlists.Current)
            {
                if (!users.Contains(playlist.Author))
                {
                    updateStatus($"Playlist {playlist.Name} does not belong to anyone, deleting it...");
                    deletedPlaylists++;
                    await DatabaseManager.PlaylistCollection.DeleteOneAsync(x => x.Id == playlist.Id);
                    continue;
                }

                foreach (string videoId in playlist.VideoIds)
                    if (!videos.Contains(videoId))
                        videos.Add(videoId);
            }

        IMongoQueryable<DatabaseChannel> cachedChannels = DatabaseManager.ChannelCacheCollection.AsQueryable();
        foreach (DatabaseChannel channel in cachedChannels)
        {
            if (!channels.Contains(channel.ChannelId))
            {
                await DatabaseManager.ChannelCacheCollection.DeleteOneAsync(x => x.ChannelId == channel.ChannelId);
                deletedChannels++;
            }
        }

        updateStatus($"Deleted {deletedChannels} channels from the cache");

        IMongoQueryable<DatabaseVideo> cachedVideos = DatabaseManager.VideoCacheCollection.AsQueryable();
        foreach (DatabaseVideo video in cachedVideos)
        {
            if (!videos.Contains(video.Id))
            {
                await DatabaseManager.VideoCacheCollection.DeleteOneAsync(x => x.Id == video.Id);
                deletedVideos++;
            }
        }

        updateStatus($"Deleted {deletedVideos} videos from the cache");

        IMongoQueryable<DatabaseLogin> tokens = DatabaseManager.TokensCollection.AsQueryable();
        foreach (DatabaseLogin login in tokens)
        {
            if (!users.Contains(login.UserID))
            {
                await DatabaseManager.TokensCollection.DeleteOneAsync(x => x.Id == login.Id);
                deletedTokens++;
            }
            // 10 days difference between creation & last seen
            else if (login.LastSeen.Subtract(login.Created).CompareTo(TimeSpan.FromDays(10)) == 1)
            {
                // if token is older than 30 days
                if (login.LastSeen >= DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(30))) continue;
                await DatabaseManager.TokensCollection.DeleteOneAsync(x => x.Id == login.Id);
                deletedTokens++;
            }
        }

        updateStatus($"Deleted {deletedVideos} videos from the cache");
        return
            $"deleted\n- {deletedChannels} channels\n- {deletedPlaylists} playlists\n- {deletedVideos} videos\n- {deletedTokens} tokens";
    }
}