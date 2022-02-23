using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LightTube.Contexts;
using LightTube.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using YTProxy;
using YTProxy.Models;
using ErrorContext = LightTube.Contexts.ErrorContext;

namespace LightTube.Controllers
{
	public class HomeController : Controller
	{
		private string[] BlockedHeaders =
		{
			"host"
		};

		private readonly ILogger<HomeController> _logger;
		private readonly Youtube _youtube;

		public HomeController(ILogger<HomeController> logger, Youtube youtube)
		{
			_logger = logger;
			_youtube = youtube;
		}

		public async Task<IActionResult> Index()
		{
			await _youtube.GetAllEndpoints();
			return View(new BaseContext
			{
				MobileLayout = Utils.IsClientMobile(Request)
			});
		}

		[Route("/feed/subscriptions")]
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

		[Route("/feed/explore")]
		public async Task<IActionResult> Explore()
		{
			return View(new BaseContext
			{
				MobileLayout = Utils.IsClientMobile(Request)
			});
		}

		[Route("/proxy")]
		public async Task Proxy(string url)
		{
			if (!url.StartsWith("http://") && !url.StartsWith("https://"))
				url = "https://" + url;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			request.Method = Request.Method;

			foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
				!header.Key.StartsWith(":") && !BlockedHeaders.Contains(header.Key.ToLower())))
				foreach (string value in values)
					request.Headers.Add(header, value);

			HttpWebResponse response = null;

			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException e)
			{
				response = e.Response as HttpWebResponse;
			}
			
			if (response == null) 
				await Response.StartAsync();

			foreach (string header in response.Headers.AllKeys)
				if (Response.Headers.ContainsKey(header))
					Response.Headers[header] = response.Headers.Get(header);
				else
					Response.Headers.Add(header, response.Headers.Get(header));
			Response.StatusCode = (int)response.StatusCode;

			await using Stream stream = response.GetResponseStream();
			try
			{
				await stream.CopyToAsync(Response.Body, HttpContext.RequestAborted);
			}
			catch (Exception)
			{
			}

			await Response.StartAsync();
		}

		[Route("/subtitle_proxy")]
		public async Task SubtitleProxy(string url)
		{
			if (!url.StartsWith("http://") && !url.StartsWith("https://"))
				url = "https://" + url;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
				!header.Key.StartsWith(":") && !BlockedHeaders.Contains(header.Key.ToLower())))
				foreach (string value in values)
					request.Headers.Add(header, value);

			using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			response.Headers.Add("Content-Type", "text/vtt");

			await using Stream stream = response.GetResponseStream();
			await stream.CopyToAsync(Response.Body);
			await Response.StartAsync();
		}

		[Route("/manifest/{v}.mpd")]
		public async Task<IActionResult> DashManifest(string v)
		{
			YoutubePlayer player = await _youtube.GetPlayerAsync(v);
			string manifest = player.GetMpdManifest($"https://{Request.Host}/proxy?url=");
			return File(Encoding.UTF8.GetBytes(manifest), "application/dash+xml");
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorContext
			{
				Path = HttpContext.Features.Get<IExceptionHandlerPathFeature>().Path,
				MobileLayout = Utils.IsClientMobile(Request)
			});
		}
	}
}