using System.Linq;
using System.Threading.Tasks;
using LightTube.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using YTProxy;

namespace LightTube.Controllers
{
	[Route("/feed")]
	public class FeedController : Controller
	{
		private readonly ILogger<FeedController> _logger;
		private readonly Youtube _youtube;

		public FeedController(ILogger<FeedController> logger, Youtube youtube)
		{
			_logger = logger;
			_youtube = youtube;
		}

		[Route("subscriptions")]
		public async Task<IActionResult> Subscriptions()
		{
			if (!HttpContext.Request.Cookies.TryGetValue("token", out string token))
				return Redirect("/Account/Login");

			try
			{
				LTUser user = await DatabaseManager.GetUserFromToken(token);
				FeedContext context = new()
				{
					Channels = user.SubscribedChannels.Select(DatabaseManager.GetChannel).ToArray(),
					Videos = await YoutubeRSS.GetMultipleFeeds(user.SubscribedChannels),
					MobileLayout = Utils.IsClientMobile(Request)
				};
				return View(context);
			}
			catch
			{
				HttpContext.Response.Cookies.Delete("token");
				return Redirect("/Account/Login");
			}
		}

		[Route("channels")]
		public async Task<IActionResult> Channels()
		{
			if (!HttpContext.Request.Cookies.TryGetValue("token", out string token))
				return Redirect("/Account/Login");

			try
			{
				LTUser user = await DatabaseManager.GetUserFromToken(token);
				FeedContext context = new()
				{
					Channels = user.SubscribedChannels.Select(DatabaseManager.GetChannel).ToArray(),
					Videos = null,
					MobileLayout = Utils.IsClientMobile(Request)
				};
				return View(context);
			}
			catch
			{
				HttpContext.Response.Cookies.Delete("token");
				return Redirect("/Account/Login");
			}
		}

		[Route("explore")]
		public async Task<IActionResult> Explore()
		{
			return View(new BaseContext
			{
				MobileLayout = Utils.IsClientMobile(Request)
			});
		}
	}
}