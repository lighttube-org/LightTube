using System.Net;
using InnerTube.Renderers;
using LightTube.ApiModels;
using LightTube.Attributes;
using LightTube.Database;
using LightTube.Database.Models;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers;

[Route("/api")]
[ApiDisableable]
public class OauthApiController : Controller
{
	private readonly InnerTube.InnerTube _youtube;

	public OauthApiController(InnerTube.InnerTube youtube)
	{
		_youtube = youtube;
	}

	private ApiResponse<T> Error<T>(string message, int code,
		HttpStatusCode statusCode)
	{
		Response.StatusCode = (int)statusCode;
		return new ApiResponse<T>(statusCode switch
			{
				HttpStatusCode.BadRequest => "BAD_REQUEST",
				HttpStatusCode.Unauthorized => "UNAUTHORIZED",
				_ => "ERROR"
			},
			message, code);
	}

	[Route("currentUser")]
	[ApiAuthorization]
	public async Task<IActionResult> GetCurrentUser()
	{
		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		if (user is null) return Unauthorized();

		return Json(user);
	}

	[Route("playlists")]
	[HttpGet]
	[ApiAuthorization("playlists.read")]
	public async Task<ApiResponse<IEnumerable<IRenderer>>> GetPlaylists()
	{
		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		if (user is null) return Error<IEnumerable<IRenderer>>("Unauthorized", 401, HttpStatusCode.Unauthorized);

		IEnumerable<DatabasePlaylist> playlists =
			DatabaseManager.Playlists.GetUserPlaylists(user.UserID, PlaylistVisibility.PRIVATE);

		ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
		return new ApiResponse<IEnumerable<IRenderer>>(user.PlaylistRenderers(PlaylistVisibility.PRIVATE).Items,
			userData);
	}

	[Route("subscriptions")]
	[HttpGet]
	[ApiAuthorization("subscriptions.read")]
	public async Task<ApiResponse<Dictionary<string, ApiSubscriptionInfo>>> GetSubscriptions()
	{
		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		if (user is null)
			return Error<Dictionary<string, ApiSubscriptionInfo>>("Unauthorized", 401, HttpStatusCode.Unauthorized);

		ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
		return new ApiResponse<Dictionary<string, ApiSubscriptionInfo>>(
			user.Subscriptions.ToDictionary(
				x => x.Key,
				x => new ApiSubscriptionInfo(x.Value))
			, userData);
	}

	[Route("feed")]
	[HttpGet]
	[ApiAuthorization("subscriptions.read")]
	public async Task<ApiResponse<FeedVideo[]>> GetSubscriptionFeed(
		bool includeNonNotification = true, int limit = 10, int skip = 0)
	{
		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		if (user is null)
			return Error<FeedVideo[]>("Unauthorized", 401, HttpStatusCode.Unauthorized);

		FeedVideo[] feed = includeNonNotification
			? await YoutubeRSS.GetMultipleFeeds(user.Subscriptions.Keys)
			: await YoutubeRSS.GetMultipleFeeds(user.Subscriptions.Where(x =>
				x.Value == SubscriptionType.NOTIFICATIONS_OFF).Select(x => x.Key));

		feed = feed.Skip(skip).Take(limit).ToArray();

		ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
		return new ApiResponse<FeedVideo[]>(feed, userData);
	}
}