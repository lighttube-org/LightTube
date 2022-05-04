using System;
using System.Linq;
using System.Threading.Tasks;
using LightTube.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InnerTube;
using InnerTube.Models;
using LightTube.Database;

namespace LightTube.Controllers
{
	public class YoutubeController : Controller
	{
		private readonly ILogger<YoutubeController> _logger;
		private readonly Youtube _youtube;

		public YoutubeController(ILogger<YoutubeController> logger, Youtube youtube)
		{
			_logger = logger;
			_youtube = youtube;
		}

		[Route("/watch")]
		public async Task<IActionResult> Watch(string v, string quality = null)
		{
			Task[] tasks = {
				_youtube.GetPlayerAsync(v, HttpContext.GetLanguage(), HttpContext.GetRegion()),
				_youtube.GetVideoAsync(v, HttpContext.GetLanguage(), HttpContext.GetRegion()),
				ReturnYouTubeDislike.GetDislikes(v)
			};
			await Task.WhenAll(tasks);

			bool cookieCompatibility = false;
			if (Request.Cookies.TryGetValue("compatibility", out string compatibilityString))
				bool.TryParse(compatibilityString, out cookieCompatibility);

			PlayerContext context = new()
			{
				Player = (tasks[0] as Task<YoutubePlayer>)?.Result,
				Video = (tasks[1] as Task<YoutubeVideo>)?.Result,
				Engagement = (tasks[2] as Task<YoutubeDislikes>)?.Result,
				Resolution = quality,
				MobileLayout = Utils.IsClientMobile(Request),
				CompatibilityMode = cookieCompatibility
			};
			return View(context);
		}

		[Route("/download")]
		public async Task<IActionResult> Download(string v)
		{
			Task[] tasks = {
				_youtube.GetPlayerAsync(v, HttpContext.GetLanguage(), HttpContext.GetRegion()),
				_youtube.GetVideoAsync(v, HttpContext.GetLanguage(), HttpContext.GetRegion()),
				ReturnYouTubeDislike.GetDislikes(v)
			};
			await Task.WhenAll(tasks);

			bool cookieCompatibility = false;
			if (Request.Cookies.TryGetValue("compatibility", out string compatibilityString))
				bool.TryParse(compatibilityString, out cookieCompatibility);

			PlayerContext context = new()
			{
				Player = (tasks[0] as Task<YoutubePlayer>)?.Result,
				Video = (tasks[1] as Task<YoutubeVideo>)?.Result,
				Engagement = null,
				MobileLayout = Utils.IsClientMobile(Request),
				CompatibilityMode = cookieCompatibility
			};
			return View(context);
		}

		[Route("/embed/{v}")]
		public async Task<IActionResult> Embed(string v, string quality = null, bool compatibility = false)
		{
			Task[] tasks = {
				_youtube.GetPlayerAsync(v, HttpContext.GetLanguage(), HttpContext.GetRegion()),
				_youtube.GetVideoAsync(v, HttpContext.GetLanguage(), HttpContext.GetRegion()),
				ReturnYouTubeDislike.GetDislikes(v)
			};
			try
			{
				await Task.WhenAll(tasks);
			}
			catch { }

			
			bool cookieCompatibility = false;
			if (Request.Cookies.TryGetValue("compatibility", out string compatibilityString))
				bool.TryParse(compatibilityString, out cookieCompatibility);
			
			PlayerContext context = new()
			{
				Player = (tasks[0] as Task<YoutubePlayer>)?.Result,
				Video = (tasks[1] as Task<YoutubeVideo>)?.Result,
				Engagement = (tasks[2] as Task<YoutubeDislikes>)?.Result,
				Resolution = quality,
				CompatibilityMode = compatibility || cookieCompatibility,
				MobileLayout = Utils.IsClientMobile(Request)
			};
			return View(context);
		}

		[Route("/results")]
		public async Task<IActionResult> Search(string search_query, string continuation = null)
		{
			SearchContext context = new()
			{
				Results = string.IsNullOrWhiteSpace(search_query)
					? new YoutubeSearchResults
					{
						Refinements = Array.Empty<string>(),
						EstimatedResults = 0,
						Results = Array.Empty<DynamicItem>(),
						ContinuationKey = null
					}
					: await _youtube.SearchAsync(search_query, continuation, HttpContext.GetLanguage(),
						HttpContext.GetRegion()),
				Query = search_query,
				ContinuationKey = continuation,
				MobileLayout = Utils.IsClientMobile(Request)
			};
			return View(context);
		}

		[Route("/playlist")]
		public async Task<IActionResult> Playlist(string list, string continuation = null)
		{
			PlaylistContext context = new()
			{
				Playlist = await _youtube.GetPlaylistAsync(list, continuation, HttpContext.GetLanguage(), HttpContext.GetRegion()),
				Id = list,
				ContinuationToken = continuation,
				MobileLayout = Utils.IsClientMobile(Request)
			};
			return View(context);
		}

		[Route("/channel/{id}")]
		public async Task<IActionResult> Channel(string id, string continuation = null)
		{
			ChannelContext context = new()
			{
				Channel = await _youtube.GetChannelAsync(id, ChannelTabs.Videos, continuation, HttpContext.GetLanguage(), HttpContext.GetRegion()),
				Id = id,
				ContinuationToken = continuation,
				MobileLayout = Utils.IsClientMobile(Request)
			};
			await DatabaseManager.Channels.UpdateChannel(context.Channel.Id, context.Channel.Name, context.Channel.Subscribers,
				context.Channel.Avatars.First().Url.ToString());
			return View(context);
		}

		[Route("/shorts/{id}")]
		public IActionResult Shorts(string id)
		{
			// yea no fuck shorts
			return Redirect("/watch?v=" + id);
		}
	}
}