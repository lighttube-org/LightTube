using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InnerTube.Models;
using MongoDB.Driver;

namespace LightTube.Database
{
	public class LoginManager
	{
		private IMongoCollection<LTUser> _userCollection;
		private IMongoCollection<LTLogin> _tokenCollection;

		public LoginManager(IMongoCollection<LTUser> userCollection, IMongoCollection<LTLogin> tokenCollection)
		{
			_userCollection = userCollection;
			_tokenCollection = tokenCollection;
		}

		public async Task<LTLogin> CreateToken(string email, string password, string userAgent, IEnumerable<string> scopes)
		{
			IAsyncCursor<LTUser> users = await _userCollection.FindAsync(x => x.Email == email);
			if (!await users.AnyAsync())
				throw new KeyNotFoundException("Invalid credentials");
			LTUser user = (await _userCollection.FindAsync(x => x.Email == email)).First();
			if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
				throw new UnauthorizedAccessException("Invalid credentials");

			LTLogin login = new()
			{
				Identifier = Guid.NewGuid().ToString(),
				Email = email,
				Token = GenerateToken(256),
				UserAgent = userAgent,
				Scopes = scopes.ToArray()
			};
			await _tokenCollection.InsertOneAsync(login);
			return login;
		}

		public async Task RemoveToken(string token)
		{
			await _tokenCollection.FindOneAndDeleteAsync(t => t.Token == token);
		}

		public async Task RemoveToken(string email, string password, string identifier)
		{
			IAsyncCursor<LTUser> users = await _userCollection.FindAsync(x => x.Email == email);
			if (!await users.AnyAsync())
				throw new KeyNotFoundException("Invalid credentials");
			LTUser user = (await _userCollection.FindAsync(x => x.Email == email)).First();
			if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
				throw new UnauthorizedAccessException("Invalid credentials");

			await _tokenCollection.FindOneAndDeleteAsync(t => t.Identifier == identifier && t.Email == user.Email);
		}

		public async Task<LTUser> GetUserFromToken(string token)
		{
			string email = (await _tokenCollection.FindAsync(x => x.Token == token)).First().Email;
			return (await _userCollection.FindAsync(u => u.Email == email)).First();
		}

		public async Task<LTLogin> GetLoginFromToken(string token)
		{
			var res = await _tokenCollection.FindAsync(x => x.Token == token);
			return res.First();
		}

		public async Task<List<LTLogin>> GetAllUserTokens(string token)
		{
			string email = (await _tokenCollection.FindAsync(x => x.Token == token)).First().Email;
			return await (await _tokenCollection.FindAsync(u => u.Email == email)).ToListAsync();
		}

		public async Task<(LTChannel channel, bool subscribed)> SubscribeToChannel(LTUser user, YoutubeChannel channel)
		{
			LTChannel ltChannel = await DatabaseManager.Channels.UpdateChannel(channel.Id, channel.Name, channel.Subscribers,
				channel.Avatars.First().Url);

			if (user.SubscribedChannels.Contains(ltChannel.ChannelId))
				user.SubscribedChannels.Remove(ltChannel.ChannelId);
			else
				user.SubscribedChannels.Add(ltChannel.ChannelId);
			
			await _userCollection.ReplaceOneAsync(x => x.Email == user.Email, user);
			return (ltChannel, user.SubscribedChannels.Contains(ltChannel.ChannelId));
		}

		public async Task DeleteUser(string email, string password)
		{
			IAsyncCursor<LTUser> users = await _userCollection.FindAsync(x => x.Email == email);
			if (!await users.AnyAsync())
				throw new KeyNotFoundException("Invalid credentials");
			LTUser user = (await _userCollection.FindAsync(x => x.Email == email)).First();
			if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
				throw new UnauthorizedAccessException("Invalid credentials");

			await _userCollection.DeleteOneAsync(x => x.Email == email);
			await _tokenCollection.DeleteManyAsync(x => x.Email == email);
		}

		public async Task CreateUser(string email, string password)
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

		private string GenerateToken(int length)
		{
			string tokenAlphabet = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-+*/()[]{}";
			Random rng = new();
			StringBuilder sb = new();
			for (int i = 0; i < length; i++)
				sb.Append(tokenAlphabet[rng.Next(0, tokenAlphabet.Length)]);
			return sb.ToString();
		}
	}
}