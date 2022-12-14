using System.Diagnostics;
using System.Text;
using System.Xml;
using LightTube.Contexts;
using LightTube.Database;
using LightTube.Database.Models;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers;

[Route("/feed")]
public class FeedController : Controller
{
	[Route("subscriptions")]
    public async Task<IActionResult> Subscription()
    {
		SubscriptionsContext ctx = new(HttpContext);
		if (ctx.User is null) return Redirect("/account/login?redirectUrl=%2ffeed%2fsubscriptions");
        ctx.Videos = await YoutubeRSS.GetMultipleFeeds(ctx.User.Subscriptions.Keys);
		return View(ctx);
    }

	[Route("rss.xml")]
	public async Task<IActionResult> RssFeed() {
		if (string.IsNullOrWhiteSpace(Request.Headers.Authorization))
		{
			Response.Headers.Add("WWW-Authenticate", "Basic realm=\"Access to the personalized RSS feed\"");
			return Unauthorized();
		}

		try {
			string type = Request.Headers.Authorization.First().Split(' ').First();
			string secret = Request.Headers.Authorization.First().Split(' ').Last();
			string secretDecoded = Encoding.UTF8.GetString(Convert.FromBase64String(secret));
			string username = secretDecoded.Split(':')[0];
			string password = secretDecoded.Split(':')[1];
			DatabaseUser? user = await DatabaseManager.Users.GetUserFromUsernamePassword(username, password);
			if (user is null) throw new Exception();
			FeedVideo[] feedVideos = await YoutubeRSS.GetMultipleFeeds(user.Subscriptions.Keys);

			XmlDocument document = new();
			XmlElement rss = document.CreateElement("rss");
			rss.SetAttribute("version", "2.0");

			XmlElement channel = document.CreateElement("channel");

			XmlElement title = document.CreateElement("title");
			title.InnerText = "LightTube subscriptions RSS feed for " + user.UserID;
			channel.AppendChild(title);

			XmlElement description = document.CreateElement("description");
			description.InnerText = $"LightTube subscriptions RSS feed for {user.UserID} with {user.Subscriptions.Count} channels";
			channel.AppendChild(description);

			foreach (FeedVideo video in feedVideos.Take(15))
			{
				XmlElement item = document.CreateElement("item");

				XmlElement id = document.CreateElement("id");
				id.InnerText = $"id:video:{video.Id}";
				item.AppendChild(id);

				XmlElement vtitle = document.CreateElement("title");
				vtitle.InnerText = video.Title;
				item.AppendChild(vtitle);

				XmlElement vdescription = document.CreateElement("description");
				vdescription.InnerText = video.Description;
				item.AppendChild(vdescription);

				XmlElement link = document.CreateElement("link");
				link.InnerText = $"https://{Request.Host}/watch?v={video.Id}";
				item.AppendChild(link);

				XmlElement published = document.CreateElement("pubDate");
				published.InnerText = video.PublishedDate.ToString("R");
				item.AppendChild(published);

				XmlElement author = document.CreateElement("author");
				
				XmlElement name = document.CreateElement("name");
				name.InnerText = video.ChannelName;
				author.AppendChild(name);
				
				XmlElement uri = document.CreateElement("uri");
				uri.InnerText = $"https://{Request.Host}/channel/{video.ChannelId}";
				author.AppendChild(uri);
				
				item.AppendChild(author);
				channel.AppendChild(item);
			}

			rss.AppendChild(channel);
			
			document.AppendChild(rss);
			return File(Encoding.UTF8.GetBytes(document.OuterXml), "application/xml");
		}
		catch (Exception e)
		{
			Response.Headers.Add("WWW-Authenticate", "Basic realm=\"Access to the personalized RSS feed\"");
			return Unauthorized();
		}
	}

	[Route("channels")]
	[HttpGet]
	public IActionResult Channels() 
	{
		ChannelsContext ctx = new(HttpContext);
		if (ctx.User is null) return Redirect("/account/login?redirectUrl=%2ffeed%2fchannels");
        ctx.Channels = from v in ctx.User.Subscriptions.Keys
			.Select(DatabaseManager.Channels.GetChannel)
			.Where(x => x is not null)
			orderby v.Name
			select v;
		return View(ctx);
	}

	[Route("channels")]
	[HttpPost]
	public async Task<IActionResult> ChannelsAsync([FromForm] Dictionary<string, string> data)
	{
		BaseContext c = new(HttpContext);
		if (c.User is null) return Redirect("/account/login?redirectUrl=%2ffeed%2fchannels");
		foreach ((string id, string type) in data)
		{
			if (int.TryParse(type, out int subTypeInt))
			{
				SubscriptionType newType = (SubscriptionType)subTypeInt;
				if (c.User.Subscriptions.TryGetValue(id, out SubscriptionType oldType))
					if (newType != oldType) 
					{
						await DatabaseManager.Users.UpdateSubscription(Request.Cookies["token"] ?? "", id, newType);
					}
				else
					await DatabaseManager.Users.UpdateSubscription(Request.Cookies["token"] ?? "", id, newType);
			}
		}

		ChannelsContext ctx = new(HttpContext);
        ctx.Channels = from v in ctx.User!.Subscriptions.Keys
			.Select(DatabaseManager.Channels.GetChannel)
			.Where(x => x is not null)
			orderby v.Name
			select v;
		return View(ctx);
	}
}