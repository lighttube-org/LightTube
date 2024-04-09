using System.Data;
using System.Text;
using LightTube.Database.Models;
using MongoDB.Driver;

namespace LightTube.Database;

public class CacheManager(IMongoCollection<DatabaseChannel> channelCollection,
    IMongoCollection<DatabaseVideo> videoCollection)
{
    public IMongoCollection<DatabaseChannel> ChannelCollection = channelCollection;
    public IMongoCollection<DatabaseVideo> VideoCollection = videoCollection;

    public async Task AddChannel(DatabaseChannel channel, bool updateOnly = false)
    {
        if (await ChannelCollection.CountDocumentsAsync(x => x.ChannelId == channel.ChannelId) > 0)
            await ChannelCollection.ReplaceOneAsync(x => x.ChannelId == channel.ChannelId, channel);
        else if (!updateOnly)
            await ChannelCollection.InsertOneAsync(channel);
    }

    public DatabaseChannel? GetChannel(string id) =>
        ChannelCollection.FindSync(x => x.ChannelId == id).FirstOrDefault();

    public async Task AddVideo(DatabaseVideo video, bool updateOnly = false)
    {
        if (await VideoCollection.CountDocumentsAsync(x => x.Id == video.Id) > 0)
            await VideoCollection.ReplaceOneAsync(x => x.Id == video.Id, video);
        else if (!updateOnly)
            await VideoCollection.InsertOneAsync(video);
    }

    public DatabaseVideo? GetVideo(string id) =>
        VideoCollection.FindSync(x => x.Id == id).FirstOrDefault();
}