using System.Text;
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
		foreach ((string id, string type) in data.Where(x => x.Key.StartsWith("UC")))
			await DatabaseManager.Users.UpdateSubscription(Request.Cookies["token"] ?? "", id, (SubscriptionType)int.Parse(type));

		ChannelsContext ctx = new(HttpContext);
        ctx.Channels = from v in ctx.User!.Subscriptions.Keys
			.Select(DatabaseManager.Channels.GetChannel)
			.Where(x => x is not null)
			orderby v.Name
			select v;
		return View(ctx);
	}
}