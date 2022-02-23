using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LightTube.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LightTube.Models;
using YTProxy;
using YTProxy.Models;

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
				_youtube.GetPlayerAsync(v),
				_youtube.GetVideoAsync(v)
			};
			await Task.WhenAll(tasks);

			PlayerContext context = new()
			{
				Player = (tasks[0] as Task<YoutubePlayer>)?.Result,
				Video = (tasks[1] as Task<YoutubeVideo>)?.Result,
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
				Results = await _youtube.SearchAsync(search_query, continuation),
				Query = search_query,
				ContinuationToken = continuation,
				MobileLayout = Utils.IsClientMobile(Request)
			};
			return View(context);
		}

		[Route("/playlist")]
		public async Task<IActionResult> Playlist(string list, string continuation = null)
		{
			PlaylistContext context = new()
			{
				Playlist = await _youtube.GetPlaylistAsync(list, continuation),
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
				Channel = await _youtube.GetChannelAsync(id, continuation),
				Id = id,
				ContinuationToken = continuation,
				MobileLayout = Utils.IsClientMobile(Request)
			};
			return View(context);
		}
	}
}