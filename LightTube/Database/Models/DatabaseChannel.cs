using MongoDB.Bson.Serialization.Attributes;

namespace LightTube.Database.Models;

[BsonIgnoreExtraElements]
public class DatabaseChannel
{
	public string ChannelId;
	public string Name;
	public string Subscribers;
	public string IconUrl;
}