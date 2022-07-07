using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using InnerTube;
using InnerTube.Models;
using LightTube.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace LightTube.Controllers
{
	[Route("/api/auth")]
	public class AuthorizedApiController : Controller
	{
		private readonly Youtube _youtube;

		private IReadOnlyList<string> _scopes = new[]
		{
			"api.subscriptions.read",
			"api.subscriptions.write"
		};

		public AuthorizedApiController(Youtube youtube)
		{
			_youtube = youtube;
		}

		private IActionResult Xml(XmlNode xmlDocument, HttpStatusCode statusCode)
		{
			MemoryStream ms = new();
			ms.Write(Encoding.UTF8.GetBytes(xmlDocument.OuterXml));
			ms.Position = 0;
			HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
			Response.StatusCode = (int)statusCode;
			return File(ms, "application/xml");
		}

		private IActionResult Error(string message, HttpStatusCode statusCode)
		{
			if (Request.Headers["Accept"].ToString().Contains("application/json"))
			{
				Response.StatusCode = (int)statusCode;
				return Json(new Dictionary<string, string>
				{
					["error"] = message
				});
			}

			XmlDocument doc = new();
			XmlElement error = doc.CreateElement("Error");
			error.InnerText = message;
			doc.AppendChild(error);
			return Xml(doc, statusCode);
		}

		private string ValidateRequest()
		{
			if (!Request.Headers.TryGetValue("X-LightTube-Client-Name", out StringValues clientName) ||
			    !Request.Headers.TryGetValue("X-LightTube-Client-Description", out StringValues clientDescription))
			{
				return
					"Missing client identifiers. See: https://gitlab.com/kuylar/lighttube/-/wikis/Authorized-API/Requirements";
			}

			if (clientName.ToString().Length > 50)
				return "X-LightTube-Client-Name cant be longer longer than 50 characters.";

			if (clientDescription.ToString().Length > 255)
				return "X-LightTube-Client-Description cant be longer longer than 255 characters.";
			
			if (clientName.Contains("|"))
				return "Invalid character '|' in X-LightTube-Client-Name";
			
			if (clientDescription.Contains("|"))
				return "Invalid character '|' in X-LightTube-Client-Description";

			return $"APIAPP|{clientName}|{clientDescription}";
		}

		[HttpPost]
		[Route("getToken")]
		public async Task<IActionResult> GetToken()
		{
			string apiInfo = ValidateRequest();
			if (!apiInfo.StartsWith("APIAPP|")) return Error(apiInfo, HttpStatusCode.BadRequest);

			if (!Request.Form.TryGetValue("user", out StringValues user))
				return Error("Missing request value: 'user'", HttpStatusCode.BadRequest);
			if (!Request.Form.TryGetValue("password", out StringValues password))
				return Error("Missing request value: 'password'", HttpStatusCode.BadRequest);
			if (!Request.Form.TryGetValue("scopes", out StringValues scopes))
				return Error("Missing request value: 'scopes'", HttpStatusCode.BadRequest);

			string[] newScopes = scopes.First().Split(",");
			foreach (string s in newScopes)
				if (!_scopes.Contains(s))
					return Error($"Unknown scope '{s}'", HttpStatusCode.BadRequest);

			try
			{
				LTLogin ltLogin =
					await DatabaseManager.Logins.CreateToken(user, password, apiInfo,
						scopes.First().Split(","));
				Response.StatusCode = (int)HttpStatusCode.Created;
 return Request.Headers["Accept"].ToString().Contains("application/json") ? Json(ltLogin) : Xml(ltLogin.GetXmlElement(), HttpStatusCode.Created);
			}
			catch (UnauthorizedAccessException)
			{
				return Error("Invalid credentials", HttpStatusCode.Unauthorized);
			}
			catch (InvalidOperationException)
			{
				return Error("User has API access disabled", HttpStatusCode.Forbidden);
			}
		}

		[Route("subscriptions/feed")]
		public async Task<IActionResult> SubscriptionsFeed()
		{
			if (!HttpContext.TryGetUser(out LTUser user, "api.subscriptions.read"))
				return Error("Unauthorized", HttpStatusCode.Unauthorized);

			SubscriptionFeed feed = new()
			{
				videos = await YoutubeRSS.GetMultipleFeeds(user.SubscribedChannels)
			};

			Response.StatusCode = (int)HttpStatusCode.OK;
 return Request.Headers["Accept"].ToString().Contains("application/json") ? Json(feed) : Xml(feed.GetXmlDocument(), HttpStatusCode.OK);
		}

		[HttpGet]
		[Route("subscriptions/channels")]
		public IActionResult SubscriptionsChannels()
		{
			if (!HttpContext.TryGetUser(out LTUser user, "api.subscriptions.read"))
				return Error("Unauthorized", HttpStatusCode.Unauthorized);

			SubscriptionChannels feed = new()
			{
				Channels = user.SubscribedChannels.Select(DatabaseManager.Channels.GetChannel).ToArray()
			};
			Array.Sort(feed.Channels, (p, q) => string.Compare(p.Name, q.Name, StringComparison.OrdinalIgnoreCase));

			Response.StatusCode = (int)HttpStatusCode.OK;
 return Request.Headers["Accept"].ToString().Contains("application/json") ? Json(feed.Channels) : Xml(feed.GetXmlDocument(), HttpStatusCode.OK);
		}

		[HttpPut]
		[Route("subscriptions/channels")]
		public async Task<IActionResult> Subscribe()
		{
			if (!HttpContext.TryGetUser(out LTUser user, "api.subscriptions.write"))
				return Error("Unauthorized", HttpStatusCode.Unauthorized);

			Request.Form.TryGetValue("id", out StringValues ids);
			string id = ids.ToString();

			if (user.SubscribedChannels.Contains(id))
				return StatusCode((int)HttpStatusCode.NotModified);

			try
			{
				YoutubeChannel channel = await _youtube.GetChannelAsync(id);

				if (channel.Id is null)
					return StatusCode((int)HttpStatusCode.NotFound);
				
				(LTChannel ltChannel, bool _) = await DatabaseManager.Logins.SubscribeToChannel(user, channel);

				return Request.Headers["Accept"].ToString().Contains("application/json") ? Json(ltChannel) : Xml(ltChannel.GetXmlDocument(), HttpStatusCode.OK);
			}
			catch (Exception e)
			{
				return Error(e.Message, HttpStatusCode.InternalServerError);
			}
		}

		[HttpDelete]
		[Route("subscriptions/channels")]
		public async Task<IActionResult> Unsubscribe()
		{
			if (!HttpContext.TryGetUser(out LTUser user, "api.subscriptions.write"))
				return Error("Unauthorized", HttpStatusCode.Unauthorized);

			Request.Form.TryGetValue("id", out StringValues ids);
			string id = ids.ToString();

			if (!user.SubscribedChannels.Contains(id))
				return StatusCode((int)HttpStatusCode.NotModified);

			try
			{
				YoutubeChannel channel = await _youtube.GetChannelAsync(id);

				if (channel.Id is null)
					return StatusCode((int)HttpStatusCode.NotFound);
				
				(LTChannel ltChannel, bool _) = await DatabaseManager.Logins.SubscribeToChannel(user, channel);

				return Request.Headers["Accept"].ToString().Contains("application/json") ? Json(ltChannel) : Xml(ltChannel.GetXmlDocument(), HttpStatusCode.OK);
			}
			catch (Exception e)
			{
				return Error(e.Message, HttpStatusCode.InternalServerError);
			}
		}
	}
}