using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace LightTube.Database
{
	[BsonIgnoreExtraElements]
	public class LTUser
	{
		public string Email;
		public string PasswordHash;
		public List<string> SubscribedChannels;
		public bool ApiAccess;
	}
}