using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using YTProxy.Models;

namespace LightTube
{
	public static class DatabaseManager
	{
		private static IMongoCollection<LTUser> _userCollection;
		private static IMongoCollection<LTLogin> _tokenCollection;
		private static IMongoCollection<LTChannel> _channelCacheCollection;

		public static void Init(string connstr)
		{
			MongoClient client = new(connstr);
			IMongoDatabase database = client.GetDatabase("lighttube");
			_userCollection = database.GetCollection<LTUser>("users");
			_tokenCollection = database.GetCollection<LTLogin>("tokens");
			_channelCacheCollection = database.GetCollection<LTChannel>("channelCache");
		}

		public static async Task<LTLogin> CreateToken(string email, string password, string userAgent)
		{
			IAsyncCursor<LTUser> users = await _userCollection.FindAsync(x => x.Email == email);
			if (!await users.AnyAsync())
				throw new KeyNotFoundException("No users found with that email");
			LTUser user = (await _userCollection.FindAsync(x => x.Email == email)).First();
			if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
				throw new UnauthorizedAccessException("Password is not correct");

			LTLogin login = new()
			{
				Identifier = Guid.NewGuid().ToString(),
				Email = email,
				Token = GenerateToken(256),
				UserAgent = userAgent
			};
			await _tokenCollection.InsertOneAsync(login);
			return login;
		}

		public static async Task RemoveToken(string token)
		{
			await _tokenCollection.FindOneAndDeleteAsync(t => t.Token == token);
		}

		public static async Task RemoveToken(string email, string password, string identifier)
		{
			IAsyncCursor<LTUser> users = await _userCollection.FindAsync(x => x.Email == email);
			if (!await users.AnyAsync())
				throw new KeyNotFoundException("No users found with that email");
			LTUser user = (await _userCollection.FindAsync(x => x.Email == email)).First();
			if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
				throw new UnauthorizedAccessException("Password is not correct");

			await _tokenCollection.FindOneAndDeleteAsync(t => t.Identifier == identifier && t.Email == user.Email);
		}

		public static async Task<LTUser> GetUserFromToken(string token)
		{
			string email = (await _tokenCollection.FindAsync(x => x.Token == token)).First().Email;
			return (await _userCollection.FindAsync(u => u.Email == email)).First();
		}

		public static async Task<List<LTLogin>> GetAllUserTokens(string token)
		{
			string email = (await _tokenCollection.FindAsync(x => x.Token == token)).First().Email;
			return await (await _tokenCollection.FindAsync(u => u.Email == email)).ToListAsync();
		}

		public static async Task<(LTChannel channel, bool subscribed)> SubscribeToChannel(string token, YoutubeChannel channel)
		{
			LTUser user = await GetUserFromToken(token);
			LTChannel ltChannel = await UpdateChannel(channel.Id, channel.Name, channel.Subscribers,
				channel.Avatars.First().Url.ToString());

			if (user.SubscribedChannels.Contains(ltChannel.ChannelId))
				user.SubscribedChannels.Remove(ltChannel.ChannelId);
			else
				user.SubscribedChannels.Add(ltChannel.ChannelId);
			
			await _userCollection.ReplaceOneAsync(x => x.Email == user.Email, user);
			return (ltChannel, user.SubscribedChannels.Contains(ltChannel.ChannelId));
		}

		public static async Task DeleteUser(string email, string password)
		{
			IAsyncCursor<LTUser> users = await _userCollection.FindAsync(x => x.Email == email);
			if (!await users.AnyAsync())
				throw new KeyNotFoundException("No users found with that email");
			LTUser user = (await _userCollection.FindAsync(x => x.Email == email)).First();
			if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
				throw new UnauthorizedAccessException("Password is not correct");

			await _userCollection.DeleteOneAsync(x => x.Email == email);
			await _tokenCollection.DeleteManyAsync(x => x.Email == email);
		}

		public static async Task CreateUser(string email, string password)
		{
			IAsyncCursor<LTUser> users = await _userCollection.FindAsync(x => x.Email == email);
			if (await users.AnyAsync())
				throw new DuplicateNameException("A user with that email already exists");

			LTUser user = new()
			{
				Email = email,
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
				SubscribedChannels = new List<string>()
			};
			await _userCollection.InsertOneAsync(user);
		}

		public static LTChannel GetChannel(string id) => _channelCacheCollection.FindSync(x => x.ChannelId == id).First();

		public static async Task<LTChannel> UpdateChannel(string id, string name, string subscribers, string iconUrl)
		{
			LTChannel channel = new()
			{
				ChannelId = id,
				Name = name,
				Subscribers = subscribers,
				IconUrl = iconUrl
			};
			if (await _channelCacheCollection.CountDocumentsAsync(x => x.ChannelId == id) > 0)
				await _channelCacheCollection.ReplaceOneAsync(x => x.ChannelId == id, channel);
			else
				await _channelCacheCollection.InsertOneAsync(channel);

			return channel;
		}

		private static string GenerateToken(int length)
		{
			string tokenAlphabet = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-+*/()[]{}";
			Random rng = new();
			StringBuilder sb = new();
			for (int i = 0; i < length; i++)
				sb.Append(tokenAlphabet[rng.Next(0, tokenAlphabet.Length)]);
			return sb.ToString();
		}
	}

	[BsonIgnoreExtraElements]
	public class LTUser
	{
		public string Email;
		public string PasswordHash;
		public List<string> SubscribedChannels;
	}

	[BsonIgnoreExtraElements]
	public class LTLogin
	{
		public string Identifier;
		public string Email;
		public string Token;
		public string UserAgent;
	}

	[BsonIgnoreExtraElements]
	public class LTChannel
	{
		public string ChannelId;
		public string Name;
		public string Subscribers;
		public string IconUrl;
	}
}