using System.Data;
using System.Text;
using LightTube.Database.Models;
using MongoDB.Driver;

namespace LightTube.Database;

public class ChannelManager
{
	public IMongoCollection<DatabaseChannel> ChannelCollection;

	public ChannelManager(IMongoCollection<DatabaseChannel> channelCollection)
	{
		ChannelCollection = channelCollection;
	}

	public DatabaseChannel? GetChannel(string id) =>
		ChannelCollection.FindSync(x => x.ChannelId == id).FirstOrDefault();
}