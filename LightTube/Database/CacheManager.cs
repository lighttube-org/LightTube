using System.Data;
using System.Text;
using LightTube.Database.Models;
using MongoDB.Driver;

namespace LightTube.Database;

public class CacheManager
{
	public IMongoCollection<DatabaseChannel> ChannelCollection;
	public IMongoCollection<DatabaseVideo> VideoCollection;

	public CacheManager(IMongoCollection<DatabaseChannel> channelCollection,
		IMongoCollection<DatabaseVideo> videoCollection)
	{
		ChannelCollection = channelCollection;
		VideoCollection = videoCollection;
	}

	public DatabaseChannel? GetChannel(string id) =>
		ChannelCollection.FindSync(x => x.ChannelId == id).FirstOrDefault();

	public async Task AddVideo(DatabaseVideo video)
	{
		if (await VideoCollection.CountDocumentsAsync(x => x.Id == video.Id) > 0)
			await VideoCollection.ReplaceOneAsync(x => x.Id == video.Id, video);
		else
			await VideoCollection.InsertOneAsync(video);
	}

	public DatabaseVideo? GetVideo(string id) =>
		VideoCollection.FindSync(x => x.Id == id).FirstOrDefault();
}