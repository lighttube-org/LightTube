using System.Net;
using System.Text.RegularExpressions;
using InnerTube;
using InnerTube.Models;
using InnerTube.Protobuf.Params;
using InnerTube.Protobuf.Responses;
using InnerTube.Renderers;
using LightTube.ApiModels;
using LightTube.Attributes;
using LightTube.Database;
using LightTube.Database.Models;
using LightTube.Localization;
using Microsoft.AspNetCore.Mvc;
using Endpoint = InnerTube.Protobuf.Endpoint;

namespace LightTube.Controllers;

[Route("/api")]
public partial class ApiController(SimpleInnerTubeClient innerTube) : Controller
{
	private readonly Regex videoIdRegex = VideoIdRegex();

	[Route("info")]
	public LightTubeInstanceInfo GetInstanceInfo() =>
		new()
		{
			Type = "lighttube/2.0",
			Version = Utils.GetVersion(),
			Messages = Configuration.Messages,
			Alert = Configuration.Alert,
			Config = new Dictionary<string, object>
			{
				["allowsApi"] = Configuration.ApiEnabled,
				["allowsNewUsers"] = Configuration.RegistrationEnabled,
				["allowsOauthApi"] = Configuration.OauthEnabled,
				["allowsThirdPartyProxyUsage"] = Configuration.ThirdPartyProxyEnabled
			}
		};

	private ApiResponse<T> Error<T>(string message, int code, HttpStatusCode statusCode)
	{
		Response.StatusCode = (int)statusCode;
		return new ApiResponse<T>(statusCode == HttpStatusCode.BadRequest ? "BAD_REQUEST" : "ERROR",
			message, code);
	}

	[Route("player")]
	[ApiDisableable]
	public async Task<ApiResponse<InnerTubePlayer>> GetPlayerInfo(string? id, bool contentCheckOk = true)
	{
		if (id is null)
			return Error<InnerTubePlayer>("Missing video ID (query parameter `id`)", 400,
				HttpStatusCode.BadRequest);

		if (!videoIdRegex.IsMatch(id) || id.Length != 11)
			return Error<InnerTubePlayer>($"Invalid video ID: {id}", 400, HttpStatusCode.BadRequest);

		try
		{
			InnerTubePlayer player = await innerTube.GetVideoPlayerAsync(id, contentCheckOk,
				HttpContext.GetInnerTubeLanguage(),
				HttpContext.GetInnerTubeRegion());

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
	public async Task<ApiResponse<InnerTubeVideo>> GetVideoInfo(
		string? id,
		bool contentCheckOk = true,
		string? playlistId = null,
		int? playlistIndex = null,
		string? playlistParams = null)
	{
		if (id is null)
			return Error<InnerTubeVideo>("Missing video ID (query parameter `id`)", 400,
				HttpStatusCode.BadRequest);

		if (!videoIdRegex.IsMatch(id) || id.Length != 11)
			return Error<InnerTubeVideo>($"Invalid video ID: {id}", 400, HttpStatusCode.BadRequest);

		try
		{
				InnerTubeVideo video = await innerTube.GetVideoDetailsAsync(id, contentCheckOk, playlistId,
					playlistIndex, playlistParams, HttpContext.GetInnerTubeLanguage(),
					HttpContext.GetInnerTubeRegion());

				DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
				ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
				userData?.AddInfoForChannel(video.Channel.Id);
				userData?.CalculateWithRenderers(video.Recommended);

				return new ApiResponse<InnerTubeVideo>(video, userData);
		}
		catch (Exception e)
		{
			return Error<InnerTubeVideo>(e.Message, 500, HttpStatusCode.InternalServerError);
		}
	}
	
	[Route("recommendations")]
	[ApiDisableable]
	public async Task<ApiResponse<ContinuationResponse>> GetVideoRecommendations(string? id, string? continuation)
	{
		if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(continuation))
		{
			return Error<ContinuationResponse>(
				"Missing video id (query parameter `id`) or continuation token (query parameter `continuation`)",
				400, HttpStatusCode.BadRequest);
		}

		try
		{
			if (id != null)
			{
				InnerTubeVideo cont = await innerTube.GetVideoDetailsAsync(id, true, null, null, null,
					HttpContext.GetInnerTubeLanguage(), HttpContext.GetInnerTubeRegion());

				DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
				ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
				userData?.CalculateWithRenderers(cont.Recommended);

				return new ApiResponse<ContinuationResponse>(new ContinuationResponse
				{
					ContinuationToken =
						(cont.Recommended.FirstOrDefault(x => x.Type == "continuation")?.Data as
							ContinuationRendererData)
						?.ContinuationToken,
					Results = cont.Recommended.Where(x => x.Type != "continuation").ToArray()
				}, userData);
			}
			else
			{
				ContinuationResponse cont = await innerTube.ContinueVideoRecommendationsAsync(continuation!,
					HttpContext.GetInnerTubeLanguage(), HttpContext.GetInnerTubeRegion());

				DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
				ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
				userData?.CalculateWithRenderers(cont.Results);

				return new ApiResponse<ContinuationResponse>(cont, userData);
			}
		}
		catch (Exception e)
		{
			return Error<ContinuationResponse>(e.Message, 500, HttpStatusCode.InternalServerError);
		}
	}

	[Route("search")]
	[ApiDisableable]
	public async Task<ApiResponse<ApiSearchResults>> Search(string query, string? continuation = null, int? index = null)
	{
		if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(continuation))
		{
			return Error<ApiSearchResults>(
				"Missing query (query parameter `query`) or continuation token (query parameter `continuation`)",
				400, HttpStatusCode.BadRequest);
		}


		ApiSearchResults result;
		if (continuation is null)
		{
			SearchParams searchParams = Request.GetSearchParams();
			if (index != null)
				searchParams.Index = index.Value;
			InnerTubeSearchResults results = await innerTube.SearchAsync(query, searchParams,
				HttpContext.GetInnerTubeLanguage(), HttpContext.GetInnerTubeRegion());
			result = new ApiSearchResults(results, searchParams);
		}
		else
		{
			ContinuationResponse results = await innerTube.ContinueSearchAsync(continuation,
				HttpContext.GetInnerTubeLanguage(), HttpContext.GetInnerTubeRegion());
			result = new ApiSearchResults(results);
		}

		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
		userData?.CalculateWithRenderers(result.Results);

		return new ApiResponse<ApiSearchResults>(result, userData);
	}

	[Route("searchSuggestions")]
	[ApiDisableable]
	public async Task<ApiResponse<SearchAutocomplete>> SearchSuggestions(string query)
	{
	    if (string.IsNullOrWhiteSpace(query))
	        return Error<SearchAutocomplete>("Missing query (query parameter `query`)", 400,
	            HttpStatusCode.BadRequest);
	    try
	    {
	        DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
	        ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
	        return new ApiResponse<SearchAutocomplete>(await SearchAutocomplete.GetAsync(query,
	            HttpContext.GetInnerTubeLanguage(),
	            HttpContext.GetInnerTubeRegion()), userData);
	    }
	    catch (Exception e)
	    {
	        return Error<SearchAutocomplete>(e.Message, 500, HttpStatusCode.InternalServerError);
	    }
	}

	[Route("playlist")]
	[ApiDisableable]
	public async Task<ApiResponse<ApiPlaylist>> Playlist(string? id, PlaylistFilter filter = PlaylistFilter.All,
		string? continuation = null)
	{
		if (string.IsNullOrWhiteSpace(id) && continuation is null)
			return Error<ApiPlaylist>($"Invalid ID: {id}", 400, HttpStatusCode.BadRequest);

		try
		{
			DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
			ApiPlaylist result;
			if (id?.StartsWith("LT-PL") == true)
			{
				if (id.Length != 24)
					return Error<ApiPlaylist>($"Invalid playlist ID: {id}", 400, HttpStatusCode.BadRequest);

				DatabasePlaylist? playlist = DatabaseManager.Playlists.GetPlaylist(id);

				if (playlist is null)
					return Error<ApiPlaylist>("The playlist does not exist.", 404,
						HttpStatusCode.InternalServerError);

				if (playlist.Visibility == PlaylistVisibility.Private)
				{
					if (playlist.Author != user?.UserID)
						return Error<ApiPlaylist>("The playlist does not exist.", 404,
							HttpStatusCode.InternalServerError);
				}

				result = new ApiPlaylist(playlist, (await DatabaseManager.Users.GetUserFromId(playlist.Author))!,
					LocalizationManager.GetFromHttpContext(HttpContext), user);
			}
			else if (continuation is null)
			{
				InnerTubePlaylist playlist =
					await innerTube.GetPlaylistAsync(id, true, filter, HttpContext.GetInnerTubeLanguage(),
						HttpContext.GetInnerTubeRegion());
				result = new ApiPlaylist(playlist);
			}
			else
			{
				ContinuationResponse playlist = await innerTube.ContinuePlaylistAsync(continuation,
					HttpContext.GetInnerTubeLanguage(), HttpContext.GetInnerTubeRegion());
				result = new ApiPlaylist(playlist);
			}

			ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
			userData?.AddInfoForChannel(result.Sidebar?.Channel?.Id);
			userData?.CalculateWithRenderers(result.Contents);
			return new ApiResponse<ApiPlaylist>(result, userData);
		}
		catch (Exception e)
		{
			return Error<ApiPlaylist>(e.Message, 500, HttpStatusCode.InternalServerError);
		}
	}

	[Route("channel")]
	[ApiDisableable]
	public async Task<ApiResponse<ApiChannel>> Channel(string? id, ChannelTabs tab = ChannelTabs.Featured,
		string? continuation = null)
	{
		if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(continuation))
			return Error<ApiChannel>($"Invalid request: missing both `id` and `continuation`", 400,
				HttpStatusCode.BadRequest);

		if (id?[0] == '@')
		{
			ResolveUrlResponse endpoint = await innerTube.ResolveUrl("https://youtube.com/@" + id);
			if (endpoint.Endpoint.EndpointTypeCase == Endpoint.EndpointTypeOneofCase.BrowseEndpoint)
				id = endpoint.Endpoint.BrowseEndpoint.BrowseId;
		}
		else if (id?.StartsWith("UC") == false)
		{
			ResolveUrlResponse endpoint = await innerTube.ResolveUrl("https://youtube.com/c/" + id);
			if (endpoint.Endpoint.EndpointTypeCase == Endpoint.EndpointTypeOneofCase.BrowseEndpoint)
				id = endpoint.Endpoint.BrowseEndpoint.BrowseId;
		}

		try
		{
			ApiChannel response;
			if (id?.StartsWith("LT") ?? false)
			{
				DatabaseUser? localUser = await DatabaseManager.Users.GetUserFromLTId(id);
				if (localUser is null)
					return Error<ApiChannel>("This user does not exist", 404, HttpStatusCode.BadRequest);
				response = new ApiChannel(localUser, LocalizationManager.GetFromHttpContext(HttpContext));
			}
			else if (continuation is null && id != null)
			{
				if (!id.StartsWith("UC"))
					return Error<ApiChannel>("resolveUrl not implemented yet", 501, HttpStatusCode.NotImplemented);
				//id = await innerTube.ResolveUrl(id) ?? id;

				InnerTubeChannel channel = await innerTube.GetChannelAsync(id, tab,
					HttpContext.GetInnerTubeLanguage(),
					HttpContext.GetInnerTubeRegion());
				response = new ApiChannel(channel);
			}
			else
			{
				ContinuationResponse channel = await innerTube.ContinueChannelAsync(continuation,
					HttpContext.GetInnerTubeLanguage(),
					HttpContext.GetInnerTubeRegion());
				response = new ApiChannel(channel);
			}

			DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
			ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
			userData?.AddInfoForChannel(response.Metadata?.Id);
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
	public async Task<ApiResponse<ContinuationResponse>> Comments(string? continuation, string? id,
		CommentsContext.Types.SortOrder sort = CommentsContext.Types.SortOrder.TopComments)
	{
		try
		{
			if (id != null && continuation == null)
				continuation = InnerTube.Utils.PackCommentsContinuation(id, sort);
			else if (id == null && continuation == null)
				return Error<ContinuationResponse>(
					"Invalid request, either 'continuation' or 'id' must be present", 400,
					HttpStatusCode.BadRequest);

			ContinuationResponse? comments = await innerTube.ContinueVideoCommentsAsync(continuation!);

			DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
			ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
			userData?.CalculateWithRenderers(comments.Results);

			return new ApiResponse<ContinuationResponse>(comments, userData);
		}
		catch (Exception e)
		{
			return Error<ContinuationResponse>(e.Message, 500, HttpStatusCode.InternalServerError);
		}
	}

	[Route("locals")]
	[ApiDisableable]
	public async Task<ApiResponse<ApiLocals>> Locals() => new(Utils.GetLocals(),
		ApiUserData.GetFromDatabaseUser(await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request)));
    [GeneratedRegex("[a-zA-Z0-9_-]{11}")]
    private static partial Regex VideoIdRegex();
}