using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace LightTube.Database.Models;

[BsonIgnoreExtraElements]
public class DatabaseLogin
{
	public string Id;
	public string UserID;
	public string Token;
	[JsonIgnore] public string UserAgent;
	public string[] Scopes;
	[JsonIgnore] public DateTimeOffset Created = DateTimeOffset.MinValue;
	[JsonIgnore] public DateTimeOffset LastSeen = DateTimeOffset.MinValue;
}