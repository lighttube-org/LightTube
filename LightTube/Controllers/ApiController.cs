using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using InnerTube;
using InnerTube.Models;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers
{
	[Route("/api")]
	public class ApiController : Controller
	{
		private const string VIDEO_ID_REGEX = @"[a-zA-Z0-9_-]{11}";
		private const string CHANNEL_ID_REGEX = @"[a-zA-Z0-9_-]{24}";
		private const string PLAYLIST_ID_REGEX = @"[a-zA-Z0-9_-]{34}";
		private readonly Youtube _youtube;

		public ApiController(Youtube youtube)
		{
			_youtube = youtube;
		}

		private IActionResult Xml(XmlNode xmlDocument)
		{
			MemoryStream ms = new();
			ms.Write(Encoding.UTF8.GetBytes(xmlDocument.OuterXml));
			ms.Position = 0;
			HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
			return File(ms, "application/xml");
		}
		
		private IActionResult Error(string message, HttpStatusCode statusCode)
		{
			Response.StatusCode = (int)statusCode;
			if (Request.Headers["Accept"].ToString().Contains("application/json"))
			{
				return Json(new Dictionary<string, string>
				{
					["error"] = message
				});
			}

			XmlDocument doc = new();
			XmlElement error = doc.CreateElement("Error");
			error.InnerText = message;
			doc.AppendChild(error);
			return Xml(doc);
		}

		[Route("player")]
		public async Task<IActionResult> GetPlayerInfo(string v)
		{
			if (v is null)
				return Error("Missing YouTube ID (query parameter `v`)", HttpStatusCode.BadRequest);
		
			Regex regex = new(VIDEO_ID_REGEX);
			if (!regex.IsMatch(v) || v.Length != 11)
				return Error($"Invalid video ID: {v}", HttpStatusCode.BadRequest);

			try
			{
				YoutubePlayer player =
					await _youtube.GetPlayerAsync(v, HttpContext.GetLanguage(), HttpContext.GetRegion());
				return Request.Headers["Accept"].ToString().Contains("application/json") ? Json(player) : Xml(player.GetXmlDocument());
			}
			catch (Exception e)
			{
				return GetErrorVideoPlayer(v, e.Message);
			}
		}

		private IActionResult GetErrorVideoPlayer(string videoId, string message)
		{
			if (Request.Headers["Accept"].ToString().Contains("application/json"))
				return Error(message, HttpStatusCode.InternalServerError);

			YoutubePlayer player = new()
			{
				Id = videoId,
				Title = "",
				Description = "",
				Tags = Array.Empty<string>(),
				Channel = new Channel
				{
					Name = "",
					Id = "",
					Avatars = Array.Empty<Thumbnail>()
				},
				Duration = 0,
				Chapters = Array.Empty<Chapter>(),
				Thumbnails = Array.Empty<Thumbnail>(),
				Formats = Array.Empty<Format>(),
				AdaptiveFormats = Array.Empty<Format>(),
				Subtitles = Array.Empty<Subtitle>(),
				Storyboards = Array.Empty<string>(),
				ExpiresInSeconds = 0,
				ErrorMessage = message
			};				
			return Xml(player.GetXmlDocument());
		}

		[Route("video")]
		public async Task<IActionResult> GetVideoInfo(string v)
		{
			if (v is null)
				return Error("Missing video ID (query parameter `v`)", HttpStatusCode.BadRequest);
		
			Regex regex = new(VIDEO_ID_REGEX);
			if (!regex.IsMatch(v) || v.Length != 11)
			{
				return Error($"Invalid video ID: {v}", HttpStatusCode.BadRequest);
			}

			YoutubeVideo video = await _youtube.GetVideoAsync(v, HttpContext.GetLanguage(), HttpContext.GetRegion());
			return Request.Headers["Accept"].ToString().Contains("application/json") ? Json(video) : Xml(video.GetXmlDocument());
		}

		[Route("search")]
		public async Task<IActionResult> Search(string query, string continuation = null)
		{
			if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(continuation))
			{
				return Error($"Missing query (query parameter `query`) or continuation key (query parameter `continuation`)", HttpStatusCode.BadRequest);
			}

			YoutubeSearchResults search = await _youtube.SearchAsync(query, continuation, HttpContext.GetLanguage(),
				HttpContext.GetRegion());
			return Request.Headers["Accept"].ToString().Contains("application/json") ? Json(search) : Xml(search.GetXmlDocument());
		}

		[Route("playlist")]
		public async Task<IActionResult> Playlist(string id, string continuation = null)
		{
			Regex regex = new(PLAYLIST_ID_REGEX);
			if (!regex.IsMatch(id) || id.Length != 34) return Error($"Invalid playlist ID: {id}", HttpStatusCode.BadRequest);


			if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(continuation))
			{
				return Error($"Invalid ID: {id}", HttpStatusCode.BadRequest);
			}

			YoutubePlaylist playlist = await _youtube.GetPlaylistAsync(id, continuation, HttpContext.GetLanguage(),
				HttpContext.GetRegion());
			return Request.Headers["Accept"].ToString().Contains("application/json") ? Json(playlist) : Xml(playlist.GetXmlDocument());
		}

		[Route("channel")]
		public async Task<IActionResult> Channel(string id, ChannelTabs tab = ChannelTabs.Home,
			string continuation = null)
		{
			Regex regex = new(CHANNEL_ID_REGEX);
			if (!regex.IsMatch(id) || id.Length != 24) return Error("Invalid channel ID: " + id, HttpStatusCode.BadRequest);

			if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(continuation))
			{
				return Error($"Invalid ID: {id}", HttpStatusCode.BadRequest);
			}

			YoutubeChannel channel = await _youtube.GetChannelAsync(id, tab, continuation, HttpContext.GetLanguage(),
				HttpContext.GetRegion());
			return Request.Headers["Accept"].ToString().Contains("application/json") ? Json(channel) : Xml(channel.GetXmlDocument());
		}

		[Route("trending")]
		public async Task<IActionResult> Trending(string id, string continuation = null)
		{
			YoutubeTrends trending = await _youtube.GetExploreAsync(id, continuation,
				HttpContext.GetLanguage(),
				HttpContext.GetRegion());
			return Request.Headers["Accept"].ToString().Contains("application/json") ? Json(trending) : Xml(trending.GetXmlDocument());
		}

		[Route("locals")]
		public async Task<IActionResult> Locals()
		{
			YoutubeLocals locals = await _youtube.GetLocalsAsync(HttpContext.GetLanguage(),
				HttpContext.GetRegion());
			return Request.Headers["Accept"].ToString().Contains("application/json") ? Json(locals) : Xml(locals.GetXmlDocument());
		}
	}
}