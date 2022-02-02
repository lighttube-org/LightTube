using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LightTube.Contexts;
using LightTube.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using YTProxy;

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
			IEnumerable<string> endpoints = await _youtube.GetAllEndpoints();
			if (!HttpContext.Request.Cookies.ContainsKey("token"))
				return View(endpoints);
			try
			{
				return Redirect("/feed/subscriptions");
			}
			catch
			{
				return View(endpoints);
			}
		}

		[Route("/feed/subscriptions")]
		public async Task<IActionResult> Feed()
		{
			if (!HttpContext.Request.Cookies.TryGetValue("token", out string token))
				return Redirect("/");

			try
			{
				LTUser user = await DatabaseManager.GetUserFromToken(token);
				FeedContext context = new()
				{
					Channels = user.SubscribedChannels.Select(DatabaseManager.GetChannel).ToArray(),
					Videos = await YoutubeRSS.GetMultipleFeeds(user.SubscribedChannels)
				};
				return View(context);
			}
			catch
			{
				HttpContext.Response.Cookies.Delete("token");
				return Redirect("/");
			}
		}

		[Route("/proxy")]
		public async Task Proxy(string url)
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

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}