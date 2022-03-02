using System.Linq;
using System.Threading.Tasks;
using LightTube.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InnerTube;
using InnerTube.Models;

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

			PlayerContext context = new()
			{
				Player = (tasks[0] as Task<YoutubePlayer>)?.Result,
				Video = (tasks[1] as Task<YoutubeVideo>)?.Result,
				Engagement = (tasks[2] as Task<YoutubeDislikes>)?.Result,
				Resolution = quality,
				MobileLayout = Utils.IsClientMobile(Request)
			};
			return View(context);
		}

		[Route("/results")]
		public async Task<IActionResult> Search(string search_query, string continuation = null)
		{
			SearchContext context = new()
			{
				Results = await _youtube.SearchAsync(search_query, continuation, HttpContext.GetLanguage(), HttpContext.GetRegion()),
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
			await DatabaseManager.UpdateChannel(context.Channel.Id, context.Channel.Name, context.Channel.Subscribers,
				context.Channel.Avatars.First().Url.ToString());
			return View(context);
		}
	}
}