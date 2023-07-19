using System.Net;
using System.Text.RegularExpressions;
using InnerTube;
using LightTube.ApiModels;
using LightTube.Attributes;
using LightTube.Database;
using LightTube.Database.Models;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers;

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

	[Route("info")]
	public LightTubeInstanceInfo GetInstanceInfo() =>
		new()
		{
			Type = "lighttube",
			Version = Utils.GetVersion(),
			Motd = Configuration.GetVariable("LIGHTTUBE_MOTD", "Search something to get started!")!,
			AllowsApi = Configuration.GetVariable("LIGHTTUBE_DISABLE_API", "")?.ToLower() != "true",
			AllowsNewUsers = Configuration.GetVariable("LIGHTTUBE_DISABLE_REGISTRATION", "")?.ToLower() != "true",
			AllowsOauthApi = Configuration.GetVariable("LIGHTTUBE_DISABLE_OAUTH", "")?.ToLower() != "true",
			AllowsThirdPartyProxyUsage =
				Configuration.GetVariable("LIGHTTUBE_ENABLE_THIRD_PARTY_PROXY", "false")?.ToLower() == "true"
		};

	private ApiResponse<T> Error<T>(string message, int code, HttpStatusCode statusCode)
	{
		Response.StatusCode = (int)statusCode;
		return new ApiResponse<T>(statusCode == HttpStatusCode.BadRequest ? "BAD_REQUEST" : "ERROR",
			message, code);
	}

	[Route("player")]
	[ApiDisableable]
	public async Task<ApiResponse<InnerTubePlayer>> GetPlayerInfo(string? id, bool contentCheckOk = true,
		bool includeHls = false)
	{
		if (id is null)
			return Error<InnerTubePlayer>("Missing video ID (query parameter `id`)", 400,
				HttpStatusCode.BadRequest);

		Regex regex = new(VIDEO_ID_REGEX);
		if (!regex.IsMatch(id) || id.Length != 11)
			return Error<InnerTubePlayer>($"Invalid video ID: {id}", 400, HttpStatusCode.BadRequest);

		try
		{
			InnerTubePlayer player =
				await _youtube.GetPlayerAsync(id, contentCheckOk, includeHls, HttpContext.GetLanguage(),
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
	[ApiDisableable]
	public async Task<ApiResponse<InnerTubeNextResponse>> GetVideoInfo(
		string? id,
		string? playlistId = null,
		int? playlistIndex = null,
		string? playlistParams = null)
	{
		if (id is null)
			return Error<InnerTubeNextResponse>("Missing video ID (query parameter `id`)", 400,
				HttpStatusCode.BadRequest);

		Regex regex = new(VIDEO_ID_REGEX);
		if (!regex.IsMatch(id) || id.Length != 11)
			return Error<InnerTubeNextResponse>($"Invalid video ID: {id}", 400, HttpStatusCode.BadRequest);

		try
		{
			InnerTubeNextResponse video = await _youtube.GetVideoAsync(id, playlistId, playlistIndex, playlistParams,
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
	[ApiDisableable]
	public async Task<ApiResponse<ApiSearchResults>> Search(string query, string? continuation = null)
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
			SearchParams searchParams = Request.GetSearchParams();
			InnerTubeSearchResults results = await _youtube.SearchAsync(query, searchParams, HttpContext.GetLanguage(),
				HttpContext.GetRegion());
			result = new ApiSearchResults(results, searchParams);
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
	[ApiDisableable]
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
	[ApiDisableable]
	public async Task<ApiResponse<ApiPlaylist>> Playlist(string id, int? skip)
	{
		if (id.StartsWith("LT-PL"))
		{
			if (id.Length != 24)
				return Error<ApiPlaylist>($"Invalid playlist ID: {id}", 400, HttpStatusCode.BadRequest);
		}
		else
		{
			Regex regex = new(PLAYLIST_ID_REGEX);
			if (!regex.IsMatch(id) || id.Length != 34)
				return Error<ApiPlaylist>($"Invalid playlist ID: {id}", 400, HttpStatusCode.BadRequest);
		}


		if (string.IsNullOrWhiteSpace(id) && skip is null)
			return Error<ApiPlaylist>($"Invalid ID: {id}", 400, HttpStatusCode.BadRequest);

		try
		{
			DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
			ApiPlaylist result;
			if (id.StartsWith("LT-PL"))
			{
				DatabasePlaylist? playlist = DatabaseManager.Playlists.GetPlaylist(id);

				if (playlist is null)
					return Error<ApiPlaylist>("The playlist does not exist.", 500,
						HttpStatusCode.InternalServerError);

				if (playlist.Visibility == PlaylistVisibility.PRIVATE)
				{
					if (user == null)
						return Error<ApiPlaylist>("The playlist does not exist.", 500,
							HttpStatusCode.InternalServerError);

					if (playlist.Author != user.UserID)
						return Error<ApiPlaylist>("The playlist does not exist.", 500,
							HttpStatusCode.InternalServerError);
				}

				result = new ApiPlaylist(playlist);
			}
			else if (skip is null)
			{
				InnerTubePlaylist playlist =
					await _youtube.GetPlaylistAsync(id, true, HttpContext.GetLanguage(), HttpContext.GetRegion());
				result = new ApiPlaylist(playlist);
			}
			else
			{
				InnerTubeContinuationResponse playlist =
					await _youtube.ContinuePlaylistAsync(id, skip.Value, HttpContext.GetLanguage(),
						HttpContext.GetRegion());
				result = new ApiPlaylist(playlist);
			}

			ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
			userData?.AddInfoForChannel(result.Channel.Id);
			userData?.CalculateWithRenderers(result.Videos);
			return new ApiResponse<ApiPlaylist>(result, userData);
		}
		catch (Exception e)
		{
			return Error<ApiPlaylist>(e.Message, 500, HttpStatusCode.InternalServerError);
		}
	}

	[Route("channel")]
	[ApiDisableable]
	public async Task<ApiResponse<ApiChannel>> Channel(string id, ChannelTabs tab = ChannelTabs.Home,
		string? searchQuery = null, string? continuation = null)
	{
		if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(continuation))
			return Error<ApiChannel>($"Invalid request: missing both `id` and `continuation`", 400,
				HttpStatusCode.BadRequest);

		try
		{
			ApiChannel response;
			if (id.StartsWith("LT"))
			{
				DatabaseUser? localUser = await DatabaseManager.Users.GetUserFromLTId(id);
				if (localUser is null)
					throw new Exception("This user does not exist.");
				response = new ApiChannel(localUser);
			}
			else if (continuation is null)
			{
				if (!id.StartsWith("UC"))
					id = await _youtube.GetChannelIdFromVanity(id) ?? id;

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

	[Route("comments")]
	[ApiDisableable]
	public async Task<ApiResponse<InnerTubeContinuationResponse>> Comments(string continuation)
	{
		if (string.IsNullOrWhiteSpace(continuation))
			return Error<InnerTubeContinuationResponse>($"Invalid continuation", 400, HttpStatusCode.BadRequest);

		try
		{
			InnerTubeContinuationResponse comments =
				await _youtube.GetVideoCommentsAsync(continuation, HttpContext.GetLanguage(), HttpContext.GetRegion());

			DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
			ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
			userData?.CalculateWithRenderers(comments.Contents);

			return new ApiResponse<InnerTubeContinuationResponse>(comments, userData);
		}
		catch (Exception e)
		{
			return Error<InnerTubeContinuationResponse>(e.Message, 500, HttpStatusCode.InternalServerError);
		}
	}

	[Route("locals")]
	[ApiDisableable]
	public async Task<IActionResult> Locals()
	{
		InnerTubeLocals locals = await _youtube.GetLocalsAsync();
		return Json(locals);
	}
}