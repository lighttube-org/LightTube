using InnerTube;
using MongoDB.Bson.Serialization.Attributes;

namespace LightTube.Database.Models;

[BsonIgnoreExtraElements]
public class DatabaseChannel
{
	public string ChannelId;
	public string Name;
	public string Subscribers;
	public string IconUrl;

	public DatabaseChannel() {
		
	}

	public DatabaseChannel(InnerTubeChannelResponse channel)
	{
		ChannelId = channel.Header!.Id;
		Name = channel.Header!.Title;
		Subscribers = channel.Header!.SubscriberCountText;
		IconUrl = channel.Header!.Avatars.Last().Url.ToString();
	}
}