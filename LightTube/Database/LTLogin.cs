using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Humanizer;
using MongoDB.Bson.Serialization.Attributes;
using MyCSharp.HttpUserAgentParser;
using Newtonsoft.Json;

namespace LightTube.Database
{
	[BsonIgnoreExtraElements]
	public class LTLogin
	{
		public string Identifier;
		public string Email;
		public string Token;
		[JsonIgnore] public string UserAgent;
		public string[] Scopes;
		[JsonIgnore] public DateTimeOffset Created = DateTimeOffset.MinValue;
		[JsonIgnore] public DateTimeOffset LastSeen = DateTimeOffset.MinValue;

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
				scopes.AppendChild(scopeElement);
			}
			login.AppendChild(scopes);
			
			doc.AppendChild(login);
			return doc;
		}

		public string GetTitle()
		{
			if (UserAgent.StartsWith("APIAPP|"))
				return $"API App: {UserAgent.Split("|")[1]}";

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

			if (UserAgent.StartsWith("APIAPP|"))
			{
				sb.AppendLine($"App info: {HttpUtility.HtmlEncode(UserAgent.Split("|")[2])}");
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
}