using System.Net;
using System.Text.RegularExpressions;
using InnerTube;
using LightTube.ApiModels;
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

		private ApiResponse<T> Error<T>(string message, int code, HttpStatusCode statusCode)
		{
			Response.StatusCode = (int)statusCode;
			return new ApiResponse<T>(statusCode == HttpStatusCode.BadRequest ? "BAD_REQUEST" : "ERROR",
				message, code);
		}

		[Route("player")]
		public async Task<ApiResponse<InnerTubePlayer>> GetPlayerInfo(string? v, bool contentCheckOk = true,
			bool includeHls = false)
		{
			if (v is null)
				return Error<InnerTubePlayer>("Missing YouTube ID (query parameter `v`)", 400,
					HttpStatusCode.BadRequest);

			Regex regex = new(VIDEO_ID_REGEX);
			if (!regex.IsMatch(v) || v.Length != 11)
				return Error<InnerTubePlayer>($"Invalid video ID: {v}", 400, HttpStatusCode.BadRequest);

			try
			{
				InnerTubePlayer player =
					await _youtube.GetPlayerAsync(v, contentCheckOk, includeHls, HttpContext.GetLanguage(),
						HttpContext.GetRegion());

				DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
				ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
				userData?.AddInfoForChannel(player.Details.Author.Id);

				return new ApiResponse<InnerTubePlayer>(player, userData);
			}
			catch (Exception e)
			{
				return Error<InnerTubePlayer>(e.Message, 500, HttpStatusCode.InternalServerError);
			}
		}

		[Route("video")]
		public async Task<ApiResponse<InnerTubeNextResponse>> GetVideoInfo(
			string? v,
			string? playlistId = null,
			int? playlistIndex = null,
			string? playlistParams = null)
		{
			if (v is null)
				return Error<InnerTubeNextResponse>("Missing video ID (query parameter `v`)", 400,
					HttpStatusCode.BadRequest);

			Regex regex = new(VIDEO_ID_REGEX);
			if (!regex.IsMatch(v) || v.Length != 11)
				return Error<InnerTubeNextResponse>($"Invalid video ID: {v}", 400, HttpStatusCode.BadRequest);

			try
			{
				InnerTubeNextResponse video = await _youtube.GetVideoAsync(v, playlistId, playlistIndex, playlistParams,
					HttpContext.GetLanguage(), HttpContext.GetRegion());

				DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
				ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
				userData?.AddInfoForChannel(video.Channel.Id);
				userData?.CalculateWithRenderers(video.Recommended);

				return new ApiResponse<InnerTubeNextResponse>(video, userData);
			}
			catch (Exception e)
			{
				return Error<InnerTubeNextResponse>(e.Message, 500, HttpStatusCode.InternalServerError);
			}
		}

		[Route("search")]
		public async Task<ApiResponse<ApiSearchResults>> Search(string query, string? continuation = null,
			string? @params = null)
		{
			if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(continuation))
			{
				return Error<ApiSearchResults>(
					"Missing query (query parameter `query`) or continuation key (query parameter `continuation`)",
					400, HttpStatusCode.BadRequest);
			}


			ApiSearchResults result;
			if (continuation is null)
			{
				InnerTubeSearchResults results = await _youtube.SearchAsync(query, @params, HttpContext.GetLanguage(),
					HttpContext.GetRegion());
				result = new ApiSearchResults(results);
			}
			else
			{
				InnerTubeContinuationResponse results = await _youtube.ContinueSearchAsync(continuation,
					HttpContext.GetLanguage(),
					HttpContext.GetRegion());
				result = new ApiSearchResults(results);
			}

			DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
			ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
			userData?.CalculateWithRenderers(result.SearchResults);

			return new ApiResponse<ApiSearchResults>(result, userData);
		}

		[Route("searchSuggestions")]
		public async Task<ApiResponse<InnerTubeSearchAutocomplete>> SearchSuggestions(string query)
		{
			if (string.IsNullOrWhiteSpace(query))
				return Error<InnerTubeSearchAutocomplete>("Missing query (query parameter `query`)", 400,
					HttpStatusCode.BadRequest);
			try
			{
				DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
				ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
				return new ApiResponse<InnerTubeSearchAutocomplete>(await _youtube.GetSearchAutocompleteAsync(query,
					HttpContext.GetLanguage(),
					HttpContext.GetRegion()), userData);
			}
			catch (Exception e)
			{
				return Error<InnerTubeSearchAutocomplete>(e.Message, 500, HttpStatusCode.InternalServerError);
			}
		}

		[Route("playlist")]
		public async Task<ApiResponse<ApiPlaylist>> Playlist(string id, string? continuation = null)
		{
			Regex regex = new(PLAYLIST_ID_REGEX);
			if (!regex.IsMatch(id) || id.Length != 34)
				return Error<ApiPlaylist>($"Invalid playlist ID: {id}", 400, HttpStatusCode.BadRequest);


			if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(continuation))
			{
				return Error<ApiPlaylist>($"Invalid ID: {id}", 400, HttpStatusCode.BadRequest);
			}

			try
			{
				InnerTubePlaylist playlist =
					await _youtube.GetPlaylistAsync(id, true, HttpContext.GetLanguage(), HttpContext.GetRegion());
				return new ApiResponse<ApiPlaylist>(new ApiPlaylist(playlist), null);
			}
			catch (Exception e)
			{
				return Error<ApiPlaylist>(e.Message, 500, HttpStatusCode.InternalServerError);
			}
		}

		[Route("channel")]
		public async Task<ApiResponse<ApiChannel>> Channel(string id, ChannelTabs tab = ChannelTabs.Home,
			string? searchQuery = null, string? continuation = null)
		{
			Regex regex = new(CHANNEL_ID_REGEX);
			if (!regex.IsMatch(id) || id.Length != 24)
				return Error<ApiChannel>("Invalid channel ID: " + id, 400, HttpStatusCode.BadRequest);

			if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(continuation))
			{
				return Error<ApiChannel>($"Invalid ID: {id}", 400, HttpStatusCode.BadRequest);
			}

			try
			{
				ApiChannel response;
				if (continuation is null)
				{
					InnerTubeChannelResponse channel = await _youtube.GetChannelAsync(id, tab, searchQuery,
						HttpContext.GetLanguage(),
						HttpContext.GetRegion());
					response = new ApiChannel(channel);
				}
				else
				{
					InnerTubeContinuationResponse channel = await _youtube.ContinueChannelAsync(continuation);
					response = new ApiChannel(channel);
				}

				DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
				ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
				userData?.AddInfoForChannel(response.Id);
				userData?.CalculateWithRenderers(response.Contents);

				return new ApiResponse<ApiChannel>(response, userData);
			}
			catch (Exception e)
			{
				return Error<ApiChannel>(e.Message, 500, HttpStatusCode.InternalServerError);
			}
		}

		[Route("locals")]
		public async Task<IActionResult> Locals()
		{
			InnerTubeLocals locals = await _youtube.GetLocalsAsync();
			return Json(locals);
		}
	}
}