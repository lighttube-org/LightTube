using System.Net;
using System.Text.RegularExpressions;
using InnerTube;
using LightTube.Attributes;
using LightTube.Database;
using LightTube.Database.Models;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers
{
	[Route("/api")]
	public class ApiController : Controller
	{
		private const string VIDEO_ID_REGEX = @"[a-zA-Z0-9_-]{11}";
		private const string CHANNEL_ID_REGEX = @"[a-zA-Z0-9_-]{24}";
		private const string PLAYLIST_ID_REGEX = @"[a-zA-Z0-9_-]{34}";
		private readonly InnerTube.InnerTube _youtube;

		public ApiController(InnerTube.InnerTube youtube)
		{
			_youtube = youtube;
		}

		[Route("currentUser")]
		[ApiAuthorization]
		public async Task<IActionResult> GetCurrentUser()
		{
			DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
			if (user is null) return Unauthorized();

			return Json(user);
		}

		private IActionResult Error(string message, int code, HttpStatusCode statusCode)
		{
			Response.StatusCode = (int)statusCode;
			return Json(new Dictionary<string, object>
			{
				["errorMessage"] = message,
				["errorCode"] = code
			});
		}

		[Route("player")]
		public async Task<IActionResult> GetPlayerInfo(string? v, bool contentCheckOk = true, bool includeHls = false)
		{
			if (v is null)
				return Error("Missing YouTube ID (query parameter `v`)", 400, HttpStatusCode.BadRequest);

			Regex regex = new(VIDEO_ID_REGEX);
			if (!regex.IsMatch(v) || v.Length != 11)
				return Error($"Invalid video ID: {v}", 400, HttpStatusCode.BadRequest);

			try
			{
				InnerTubePlayer player =
					await _youtube.GetPlayerAsync(v, contentCheckOk, includeHls, HttpContext.GetLanguage(),
						HttpContext.GetRegion());
				return Json(player);
			}
			catch (Exception e)
			{
				return Error(e.Message, 500, HttpStatusCode.InternalServerError);
			}
		}

		[Route("video")]
		public async Task<IActionResult> GetVideoInfo(string? v, string? playlistId = null, int? playlistIndex = null,
			string? playlistParams = null)
		{
			if (v is null)
				return Error("Missing video ID (query parameter `v`)", 400, HttpStatusCode.BadRequest);

			Regex regex = new(VIDEO_ID_REGEX);
			if (!regex.IsMatch(v) || v.Length != 11)
				return Error($"Invalid video ID: {v}", 400, HttpStatusCode.BadRequest);

			InnerTubeNextResponse video = await _youtube.GetVideoAsync(v, playlistId, playlistIndex, playlistParams,
				HttpContext.GetLanguage(), HttpContext.GetRegion());
			return Json(video);
		}

		[Route("search")]
		public async Task<IActionResult> Search(string query, string? continuation = null, string? @params = null)
		{
			if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(continuation))
			{
				return Error(
					"Missing query (query parameter `query`) or continuation key (query parameter `continuation`)",
					400, HttpStatusCode.BadRequest);
			}

			return continuation is null
				? Json(await _youtube.SearchAsync(query, @params, HttpContext.GetLanguage(), HttpContext.GetRegion()))
				: Json(await _youtube.ContinueSearchAsync(continuation, HttpContext.GetLanguage(),
					HttpContext.GetRegion()));
		}

		[Route("searchSuggestions")]
		public async Task<IActionResult> SearchSuggestions(string query)
		{
			if (string.IsNullOrWhiteSpace(query))
				return Error($"Missing query (query parameter `query`)", 400, HttpStatusCode.BadRequest);

			return Json(await _youtube.GetSearchAutocompleteAsync(query, HttpContext.GetLanguage(),
				HttpContext.GetRegion()));
		}

		[Route("playlist")]
		public async Task<IActionResult> Playlist(string id, string? continuation = null)
		{
			Regex regex = new(PLAYLIST_ID_REGEX);
			if (!regex.IsMatch(id) || id.Length != 34)
				return Error($"Invalid playlist ID: {id}", 400, HttpStatusCode.BadRequest);


			if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(continuation))
			{
				return Error($"Invalid ID: {id}", 400, HttpStatusCode.BadRequest);
			}

			InnerTubePlaylist playlist =
				await _youtube.GetPlaylistAsync(id, true, HttpContext.GetLanguage(), HttpContext.GetRegion());
			return Json(playlist);
		}

		[Route("channel")]
		public async Task<IActionResult> Channel(string id, ChannelTabs tab = ChannelTabs.Home,
			string? searchQuery = null, string? continuation = null)
		{
			Regex regex = new(CHANNEL_ID_REGEX);
			if (!regex.IsMatch(id) || id.Length != 24)
				return Error("Invalid channel ID: " + id, 400, HttpStatusCode.BadRequest);

			if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(continuation))
			{
				return Error($"Invalid ID: {id}", 400, HttpStatusCode.BadRequest);
			}

			return continuation is null
				? Json(await _youtube.GetChannelAsync(id, tab, searchQuery, HttpContext.GetLanguage(),
					HttpContext.GetRegion()))
				: Json(await _youtube.ContinueChannelAsync(id));
		}

		[Route("locals")]
		public async Task<IActionResult> Locals()
		{
			InnerTubeLocals locals = await _youtube.GetLocalsAsync();
			return Json(locals);
		}
	}
}