using System.Net;
using InnerTube;
using InnerTube.Models;
using InnerTube.Protobuf;
using InnerTube.Renderers;
using LightTube.ApiModels;
using LightTube.Attributes;
using LightTube.CustomRendererDatas;
using LightTube.Database;
using LightTube.Database.Models;
using LightTube.Localization;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers;

[Route("/api")]
[ApiDisableable]
public class OauthApiController(SimpleInnerTubeClient innerTube) : Controller
{
    
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
    public async Task<ApiResponse<IEnumerable<RendererContainer>>> GetPlaylists()
    {
        DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
        if (user is null) return Error<IEnumerable<RendererContainer>>("Unauthorized", 401, HttpStatusCode.Unauthorized);

        ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
        return new ApiResponse<IEnumerable<RendererContainer>>(
            user.PlaylistRenderers(LocalizationManager.GetFromHttpContext(HttpContext), PlaylistVisibility.Private),
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
                Request.Headers.Authorization.ToString(), request.Title,
                request.Description ?? "", request.Visibility ?? PlaylistVisibility.Private);
            return new ApiResponse<DatabasePlaylist>(playlist, userData);
        }
        catch (Exception e)
        {
            return Error<DatabasePlaylist>(e.Message, 500, HttpStatusCode.InternalServerError);
        }
    }

    [Route("playlists/{id}")]
    [HttpPatch]
    [ApiAuthorization("playlists.write")]
    public async Task<ApiResponse<DatabasePlaylist>> UpdatePlaylist(string id, [FromBody] CreatePlaylistRequest request)
    {
        DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
        if (user is null) return Error<DatabasePlaylist>("Unauthorized", 401, HttpStatusCode.Unauthorized);

        if (string.IsNullOrWhiteSpace(request.Title))
            return Error<DatabasePlaylist>("Playlist title cannot be null, empty or whitespace", 400,
                HttpStatusCode.BadRequest);

        try
        {
            ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
            await DatabaseManager.Playlists.EditPlaylist(
                Request.Headers.Authorization.ToString(), id, request.Title,
                request.Description ?? "", request.Visibility ?? PlaylistVisibility.Private);
            DatabasePlaylist playlist = DatabaseManager.Playlists.GetPlaylist(id)!;
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
            await DatabaseManager.Playlists.DeletePlaylist(Request.Headers.Authorization.ToString(), id);
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
            InnerTubePlayer video = await innerTube.GetVideoPlayerAsync(videoId, true,
                HttpContext.GetInnerTubeLanguage(), HttpContext.GetInnerTubeRegion());
            ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
            await DatabaseManager.Playlists.AddVideoToPlaylist(
                Request.Headers.Authorization.ToString(),
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
                Request.Headers.Authorization.ToString(),
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
        Dictionary<string, DatabaseChannel> channels = [];
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
    public async Task<ApiResponse<IEnumerable<RendererContainer>>> GetSubscriptionFeed(
        bool includeNonNotification = true, int limit = 10, int skip = 0)
    {
        DatabaseUser? user = await DatabaseManager.Oauth2.GetUserFromHttpRequest(Request);
        if (user is null)
            return Error<IEnumerable<RendererContainer>>("Unauthorized", 401, HttpStatusCode.Unauthorized);

        Dictionary<string, string> avatars = [];
        foreach (string id in user.Subscriptions.Keys)
        {
            DatabaseChannel? channel = DatabaseManager.Cache.GetChannel(id);
            if (channel is null) continue;
            avatars.Add(id, channel.IconUrl);
        }

        FeedVideo[] feed = includeNonNotification
            ? await YoutubeRss.GetMultipleFeeds(user.Subscriptions.Keys)
            : await YoutubeRss.GetMultipleFeeds(user.Subscriptions.Where(x =>
                x.Value == SubscriptionType.NOTIFICATIONS_ON).Select(x => x.Key));

        feed = feed.Skip(skip).Take(limit).ToArray();

        IEnumerable<RendererContainer> renderers = feed.Select(x => new RendererContainer
        {
            Type = "video",
            OriginalType = "lightTubeFeedVideo",
            Data = new SubscriptionFeedVideoRendererData
            {
                VideoId = x.Id,
                Title = x.Title,
                Thumbnails =
                [
                    new Thumbnail
                    {
                        Url = x.Thumbnail,
                        Width = 0,
                        Height = 0
                    }
                ],
                Author = new Channel("en",
                    x.ChannelId,
                    x.ChannelName,
                    null,
                    avatars.TryGetValue(x.ChannelId, out string? avatarUrl)
                        ? [
                            new Thumbnail
                            {
                                Url = avatarUrl,
                                Width = 0,
                                Height = 0
                            }
                        ]
                        : null,
                    null,
                    null),
                Duration = TimeSpan.Zero,
                PublishedText = x.PublishedDate.ToString("D"),
                RelativePublishedDate = Utils.ToRelativePublishedDate(x.PublishedDate),
                ViewCountText =
                    string.Format(LocalizationManager.GetFromHttpContext(HttpContext).GetRawString("channel.about.views"), x.ViewCount.ToString("N0")),
                ViewCount = x.ViewCount,
                Badges = [],
                Description = x.Description,
                PremiereStartTime = null,
                ExactPublishDate = x.PublishedDate
            }
        });

        ApiUserData? userData = ApiUserData.GetFromDatabaseUser(user);
        return new ApiResponse<IEnumerable<RendererContainer>>(renderers, userData);
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

            InnerTubeChannel channel = await innerTube.GetChannelAsync(req.ChannelId);
            (string? channelId, SubscriptionType subscriptionType) = await DatabaseManager.Users.UpdateSubscription(
                Request.Headers.Authorization.ToString(), req.ChannelId,
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
            InnerTubeChannel channel = await innerTube.GetChannelAsync(id);
            (string? channelId, SubscriptionType type) = await DatabaseManager.Users.UpdateSubscription(
                Request.Headers.Authorization.ToString(), id,
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