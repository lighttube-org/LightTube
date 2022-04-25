using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MyCSharp.HttpUserAgentParser;
using Newtonsoft.Json;

namespace LightTube.Database
{
	public static class DatabaseManager
	{
		private static IMongoCollection<LTUser> _userCollection;
		private static IMongoCollection<LTLogin> _tokenCollection;
		private static IMongoCollection<LTChannel> _channelCacheCollection;
		public static LoginManager Logins { get; private set; }
		public static ChannelManager Channels { get; private set; }

		public static void Init(string connstr)
		{
			MongoClient client = new(connstr);
			IMongoDatabase database = client.GetDatabase("lighttube");
			_userCollection = database.GetCollection<LTUser>("users");
			_tokenCollection = database.GetCollection<LTLogin>("tokens");
			_channelCacheCollection = database.GetCollection<LTChannel>("channelCache");
			Logins = new LoginManager(_userCollection, _tokenCollection);
			Channels = new ChannelManager(_channelCacheCollection);
		}

		public static void CreateLocalAccount(this HttpContext context)
		{
			bool accountExists = false;

			// Check local account
			if (context.Request.Cookies.TryGetValue("account_data", out string accountJson))
			{
				try
				{
					if (accountJson != null)
					{
						LTUser tempUser = JsonConvert.DeserializeObject<LTUser>(accountJson) ?? new LTUser();
						if (tempUser.Email == "Local Account" && tempUser.PasswordHash == "local_account")
							accountExists = true;
					}
				}
				catch { }
			}

			// Account already exists, just leave it there
			if (accountExists) return;

			LTUser user = new()
			{
				Email = "Local Account",
				PasswordHash = "local_account",
				SubscribedChannels = new List<string>()
			};

			context.Response.Cookies.Append("account_data", JsonConvert.SerializeObject(user), new CookieOptions
			{
				Expires = DateTimeOffset.MaxValue 
			});
		}

		public static bool TryGetUser(this HttpContext context, out LTUser user, string requiredScope)
		{
			// Check local account
			if (context.Request.Cookies.TryGetValue("account_data", out string accountJson))
			{
				try
				{
					if (accountJson != null)
					{
						LTUser tempUser = JsonConvert.DeserializeObject<LTUser>(accountJson) ?? new LTUser();
						if (tempUser.Email == "Local Account" && tempUser.PasswordHash == "local_account")
						{
							user = tempUser;
							return true;
						}
					}
				}
				catch
				{
					user = null;
					return false;
				}
			}
			
			// Check cloud account
			if (!context.Request.Cookies.TryGetValue("token", out string token))
				if (context.Request.Headers.TryGetValue("Authorization", out StringValues tokens))
					token = tokens.ToString();
				else
				{
					user = null;
					return false;
				}

			try
			{
				if (token != null)
				{
					user = Logins.GetUserFromToken(token).Result;
					LTLogin login = Logins.GetLoginFromToken(token).Result;
					if (login.Scopes.Contains(requiredScope))
					{
#pragma warning disable 4014
						login.UpdateLastAccess(DateTimeOffset.Now);
#pragma warning restore 4014
						return true;
					}
					return false;
				}
			}
			catch
			{
				user = null;
				return false;
			}

			user = null;
			return false;
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
		private static string _apiUaRegex = "LightTubeApiClient\\/([0-9.]*) ([\\S]+?)\\/([0-9.]*) \\(([\\s\\S]+?)\\)";
		public string Identifier;
		public string Email;
		public string Token;
		public string UserAgent;
		public string[] Scopes;
		public DateTimeOffset Created = DateTimeOffset.MinValue;
		public DateTimeOffset LastSeen = DateTimeOffset.MinValue;

		public XmlDocument GetXmlElement()
		{
			XmlDocument doc = new();
			XmlElement login = doc.CreateElement("Login");
			login.SetAttribute("id", Identifier);
			login.SetAttribute("user", Email);

			XmlElement token = doc.CreateElement("Token");
			token.InnerText = Token;
			login.AppendChild(token);

			XmlElement scopes = doc.CreateElement("Scopes");
			foreach (string scope in Scopes)
			{
				XmlElement scopeElement = doc.CreateElement("Scope");
				scopeElement.InnerText = scope;
				login.AppendChild(scopeElement);
			}
			login.AppendChild(scopes);
			
			doc.AppendChild(login);
			return doc;
		}

		public string GetTitle()
		{
			Match match = Regex.Match(UserAgent, _apiUaRegex);
			if (match.Success)
				return $"API App: {match.Groups[2]} {match.Groups[3]}";

			HttpUserAgentInformation client = HttpUserAgentParser.Parse(UserAgent);
			StringBuilder sb = new($"{client.Name} {client.Version}");
			if (client.Platform.HasValue)
				sb.Append($" on {client.Platform.Value.PlatformType.ToString()}");
			return sb.ToString();
		}

		public string GetDescription()
		{
			StringBuilder sb = new();
			sb.AppendLine($"Created: {Created.Humanize(DateTimeOffset.Now)}");
			sb.AppendLine($"Last seen: {LastSeen.Humanize(DateTimeOffset.Now)}");

			Match match = Regex.Match(UserAgent, _apiUaRegex);
			if (match.Success)
			{
				sb.AppendLine($"API version: {HttpUtility.HtmlEncode(match.Groups[1])}");
				sb.AppendLine($"App info: {HttpUtility.HtmlEncode(match.Groups[4])}");
				sb.AppendLine("Allowed scopes:");
				foreach (string scope in Scopes) sb.AppendLine($"- {scope}");
			}

			return sb.ToString();
		}

		public async Task UpdateLastAccess(DateTimeOffset newTime)
		{
			await DatabaseManager.Logins.UpdateLastAccess(Identifier, newTime);
		}
	}

	[BsonIgnoreExtraElements]
	public class LTChannel
	{
		public string ChannelId;
		public string Name;
		public string Subscribers;
		public string IconUrl;

		public XmlNode GetXmlElement(XmlDocument doc)
		{
			XmlElement item = doc.CreateElement("Channel");
			item.SetAttribute("id", ChannelId);
			item.SetAttribute("subscribers", Subscribers);

			XmlElement title = doc.CreateElement("Name");
			title.InnerText = Name;
			item.AppendChild(title);

			XmlElement thumbnail = doc.CreateElement("Avatar");
			thumbnail.InnerText = IconUrl;
			item.AppendChild(thumbnail);

			return item;
		}
	}
}