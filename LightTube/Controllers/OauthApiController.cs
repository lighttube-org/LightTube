using System.Net;
using InnerTube;
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
	public async Task<ApiResponse<DatabaseUser>> GetCurrentUser()
	{
		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		if (user is null) return Error<DatabaseUser>("Unauthorized", 401, HttpStatusCode.Unauthorized);

		ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
		return new ApiResponse<DatabaseUser>(user, userData);
	}

	#region playlists.*

	[Route("playlists")]
	[HttpGet]
	[ApiAuthorization("playlists.read")]
	public async Task<ApiResponse<IEnumerable<IRenderer>>> GetPlaylists()
	{
		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		if (user is null) return Error<IEnumerable<IRenderer>>("Unauthorized", 401, HttpStatusCode.Unauthorized);

		ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
		return new ApiResponse<IEnumerable<IRenderer>>(user.PlaylistRenderers(PlaylistVisibility.PRIVATE).Items,
			userData);
	}

	[Route("playlists")]
	[HttpPut]
	[ApiAuthorization("playlists.write")]
	public async Task<ApiResponse<DatabasePlaylist>> CreatePlaylist([FromBody] CreatePlaylistRequest request)
	{
		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		if (user is null) return Error<DatabasePlaylist>("Unauthorized", 401, HttpStatusCode.Unauthorized);

		if (string.IsNullOrWhiteSpace(request.Title))
			return Error<DatabasePlaylist>("Playlist title cannot be null, empty or whitespace", 400,
				HttpStatusCode.BadRequest);

		try
		{
			ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
			DatabasePlaylist playlist = await DatabaseManager.Playlists.CreatePlaylist(
				Request.Headers["Authorization"].ToString(), request.Title,
				request.Description ?? "", request.Visibility ?? PlaylistVisibility.PRIVATE);
			return new ApiResponse<DatabasePlaylist>(playlist, userData);
		}
		catch (Exception e)
		{
			return Error<DatabasePlaylist>(e.Message, 500, HttpStatusCode.InternalServerError);
		}
	}

	[Route("playlists/{id}")]
	[HttpDelete]
	[ApiAuthorization("playlists.write")]
	public async Task<ApiResponse<string>> DeletePlaylist(string id)
	{
		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		if (user is null) return Error<string>("Unauthorized", 401, HttpStatusCode.Unauthorized);

		try
		{
			ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
			await DatabaseManager.Playlists.DeletePlaylist(Request.Headers["Authorization"].ToString(), id);
			return new ApiResponse<string>($"Deleted playlist '{id}'", userData);
		}
		catch (Exception e)
		{
			return Error<string>(e.Message, 500, HttpStatusCode.InternalServerError);
		}
	}

	[Route("playlists/{playlistId}/{videoId}")]
	[HttpPut]
	[ApiAuthorization("playlists.write")]
	public async Task<ApiResponse<ModifyPlaylistContentResponse>> PutVideoIntoPlaylist(string playlistId,
		string videoId)
	{
		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		if (user is null) return Error<ModifyPlaylistContentResponse>("Unauthorized", 401, HttpStatusCode.Unauthorized);

		try
		{
			InnerTubePlayer video = await _youtube.GetPlayerAsync(videoId);
			ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
			await DatabaseManager.Playlists.AddVideoToPlaylist(
				Request.Headers["Authorization"].ToString(),
				playlistId,
				video);
			return new ApiResponse<ModifyPlaylistContentResponse>(new ModifyPlaylistContentResponse(video), userData);
		}
		catch (Exception e)
		{
			return Error<ModifyPlaylistContentResponse>(e.Message, 500, HttpStatusCode.InternalServerError);
		}
	}

	[Route("playlists/{playlistId}/{videoId}")]
	[HttpDelete]
	[ApiAuthorization("playlists.write")]
	public async Task<ApiResponse<string>> DeleteVideoFromPlaylist(string playlistId, string videoId)
	{
		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		if (user is null) return Error<string>("Unauthorized", 401, HttpStatusCode.Unauthorized);

		try
		{
			ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
			await DatabaseManager.Playlists.RemoveVideoFromPlaylist(
				Request.Headers["Authorization"].ToString(),
				playlistId,
				videoId);
			return new ApiResponse<string>($"Removed '{videoId}' from playlist '{playlistId}'", userData);
		}
		catch (Exception e)
		{
			return Error<string>(e.Message, 500, HttpStatusCode.InternalServerError);
		}
	}

	#endregion

	#region subscriptions.*

	[Route("subscriptions")]
	[HttpGet]
	[ApiAuthorization("subscriptions.read")]
	public async Task<ApiResponse<Dictionary<string, DatabaseChannel>>> GetSubscriptions(string? channelId = null)
	{
		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		if (user is null)
			return Error<Dictionary<string, DatabaseChannel>>("Unauthorized", 401, HttpStatusCode.Unauthorized);

		ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
		Dictionary<string, DatabaseChannel> channels = new();
		if (string.IsNullOrEmpty(channelId))
		{
			foreach (string id in user.Subscriptions.Keys)
			{
				DatabaseChannel? channel = DatabaseManager.Cache.GetChannel(id);
				if (channel is null) continue;
				userData?.AddInfoForChannel(id);
				channels.Add(id, channel);
			}
		}
		else
		{
			userData?.AddInfoForChannel(channelId);
			DatabaseChannel? channel = DatabaseManager.Cache.GetChannel(channelId);
			if (channel is not null)
				channels.Add(channelId, channel);
		}

		return new ApiResponse<Dictionary<string, DatabaseChannel>>(channels, userData);
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

	[Route("subscriptions")]
	[HttpPut]
	[ApiAuthorization("subscriptions.write")]
	public async Task<ApiResponse<UpdateSubscriptionResponse>> UpdateSubscription(
		[FromBody] UpdateSubscriptionRequest req)
	{
		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		if (user is null)
			return Error<UpdateSubscriptionResponse>("Unauthorized", 401, HttpStatusCode.Unauthorized);

		ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);

		try
		{
			SubscriptionType type = req.Subscribed
				? req.EnableNotifications
					? SubscriptionType.NOTIFICATIONS_ON
					: SubscriptionType.NOTIFICATIONS_OFF
				: SubscriptionType.NONE;
			
			InnerTubeChannelResponse channel = await _youtube.GetChannelAsync(req.ChannelId);
			(string? channelId, SubscriptionType subscriptionType) = await DatabaseManager.Users.UpdateSubscription(
				Request.Headers["Authorization"].ToString()!, req.ChannelId,
				type);
			if (req.Subscribed)
				await DatabaseManager.Cache.AddChannel(new DatabaseChannel(channel));
			
			userData?.Channels.Add(channelId, new ApiSubscriptionInfo(type));

			return new ApiResponse<UpdateSubscriptionResponse>(
				new UpdateSubscriptionResponse(channel, subscriptionType), userData);
		}
		catch (Exception e)
		{
			return Error<UpdateSubscriptionResponse>(e.StackTrace!, 500, HttpStatusCode.InternalServerError);
		}
	}

	[Route("subscriptions/{id}")]
	[HttpDelete]
	[ApiAuthorization("subscriptions.write")]
	public async Task<ApiResponse<UpdateSubscriptionResponse>> Unsubscribe(string id)
	{
		DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
		if (user is null)
			return Error<UpdateSubscriptionResponse>("Unauthorized", 401, HttpStatusCode.Unauthorized);

		ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);

		try
		{
			InnerTubeChannelResponse channel = await _youtube.GetChannelAsync(id);
			(string? channelId, SubscriptionType type) = await DatabaseManager.Users.UpdateSubscription(
				Request.Headers["Authorization"].ToString()!, id,
				SubscriptionType.NONE);
			userData?.Channels.Add(channelId, new ApiSubscriptionInfo(type));
			return new ApiResponse<UpdateSubscriptionResponse>(new UpdateSubscriptionResponse(channel, type), userData);
		}
		catch (Exception e)
		{
			return Error<UpdateSubscriptionResponse>(e.StackTrace!, 500, HttpStatusCode.InternalServerError);
		}
	}

	#endregion
}