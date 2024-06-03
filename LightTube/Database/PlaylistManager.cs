using InnerTube;
using InnerTube.Models;
using InnerTube.Protobuf;
using InnerTube.Renderers;
using LightTube.Database.Models;
using LightTube.Localization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace LightTube.Database;

public class PlaylistManager(
    IMongoCollection<DatabasePlaylist> playlistCollection,
    IMongoCollection<DatabaseVideo> videoCacheCollection)
{
    private const string INNERTUBE_PLAYLIST_PANEL_VIDEO_RENDERER_TEMPLATE = "{\"title\":{\"simpleText\":\"%%TITLE%%\"},\"thumbnail\":{\"thumbnails\":[{\"url\":\"%%THUMBNAIL%%\",\"width\":0,\"height\":0}]},\"lengthText\":{\"simpleText\":\"%%DURATION%%\"},\"indexText\":{\"simpleText\":\"%%INDEX%%\"},\"selected\":%%SELECTED%%,\"navigationEndpoint\":{\"watchEndpoint\":{\"params\":\"OAE%3D\"}},\"videoId\":\"%%ID%%\",\"shortBylineText\":{\"runs\":[{\"text\":\"%%CHANNEL_TITLE%%\",\"navigationEndpoint\":{\"browseEndpoint\":{\"browseId\":\"%%CHANNEL_ID%%\"}}}]}}";
    public IMongoCollection<DatabasePlaylist> PlaylistCollection { get; } = playlistCollection;
    public IMongoCollection<DatabaseVideo> VideoCacheCollection { get; } = videoCacheCollection;

    public DatabasePlaylist? GetPlaylist(string id) => PlaylistCollection.FindSync(x => x.Id == id).FirstOrDefault();

    public IEnumerable<DatabasePlaylist> GetUserPlaylists(string userId, PlaylistVisibility minVisibility)
    {
        IAsyncCursor<DatabasePlaylist> unfiltered = PlaylistCollection.FindSync(x => x.Author == userId);
        return unfiltered.ToList().Where(x => x.Visibility >= minVisibility);
    }

    public IEnumerable<RendererContainer> GetPlaylistVideoRenderers(string id, bool editable, LocalizationManager localization)
    {
        DatabasePlaylist? pl = GetPlaylist(id);
        if (pl == null) return [];

        List<RendererContainer> renderers = [];

        for (int i = 0; i < pl.VideoIds.Count; i++)
        {
            string videoId = pl.VideoIds[i];
            DatabaseVideo? video = VideoCacheCollection.FindSync(x => x.Id == videoId).FirstOrDefault();
            RendererContainer container = new()
            {
                Type = "video",
                OriginalType = "playlistVideoContainer",
                Data = new PlaylistVideoRendererData
                {
                    VideoId = editable ? videoId + "!" : videoId,
                    Title = video?.Title.Replace("\"", "\\\"") ?? localization.GetRawString("playlist.video.uncached"),
                    Thumbnails =
                    [
                        new Thumbnail
                        {
                            Url = video?.Thumbnails.LastOrDefault()?.Url.ToString() ?? "https://i.ytimg.com/vi//hqdefault.jpg",
                            Width = 480,
                            Height = 360
                        }
                    ],
                    Author = video?.Channel.Id != null
                        ? new Channel("en",
                            video.Channel.Id,
                            video?.Channel.Name.Replace("\"", "\\\"") ?? "???",
                            null,
                            null,
                            null,
                            null
                        )
                        : null,
                    Duration = InnerTube.Utils.ParseDuration(video?.Duration ?? "00:00"),
                    PublishedText = video?.UploadedAt,
                    ViewCountText = (video?.Views ?? 0).ToString(),
                    Badges = [],
                    Description = null,
                    VideoIndexText = (i + 1).ToString()
                }
            };
            renderers.Add(container);
        }

        return renderers;
    }

    public List<DatabaseVideo> GetPlaylistVideos(string playlistId, LocalizationManager localization)
    {
        DatabasePlaylist? pl = GetPlaylist(playlistId);
        return pl == null
            ? []
            : pl.VideoIds.Select(id => VideoCacheCollection.FindSync(x => x.Id == id).FirstOrDefault()).ToList();
    }

    public string GetPlaylistPanelVideosJson(string id, string currentVideoId)
    {
        DatabasePlaylist? pl = GetPlaylist(id);
        if (pl == null) return "";

        List<string> renderers = [];

        for (int i = 0; i < pl.VideoIds.Count; i++)
        {
            string videoId = pl.VideoIds[i];
            DatabaseVideo? video = VideoCacheCollection.FindSync(x => x.Id == videoId).FirstOrDefault();
            string json = $"{{\"playlistPanelVideoRenderer\":{INNERTUBE_PLAYLIST_PANEL_VIDEO_RENDERER_TEMPLATE}}}"
                .Replace("%%ID%%", videoId)
                .Replace("%%SELECTED%%", (currentVideoId == videoId).ToString().ToLower())
                .Replace("%%INDEX%%", currentVideoId == videoId ? "▶" : (i + 1).ToString())
                .Replace("%%TITLE%%", video?.Title.Replace("\"", "\\\"") ?? "Uncached video. Click to fix")
                .Replace("%%THUMBNAIL%%", video?.Thumbnails.LastOrDefault()?.Url.ToString() ?? "https://i.ytimg.com/vi//hqdefault.jpg")
                .Replace("%%DURATION%%", video?.Duration ?? "00:00")
                .Replace("%%CHANNEL_TITLE%%", video?.Channel.Name.Replace("\"", "\\\"") ?? "???")
                .Replace("%%CHANNEL_ID%%", video?.Channel.Id ?? "???");
            renderers.Add(json);
        }

        return string.Join(",", renderers);
    }

    public async Task<DatabasePlaylist> CreatePlaylist(string token, string title, string description, PlaylistVisibility visibility)
    {
        DatabaseUser? u = await DatabaseManager.Users.GetUserFromToken(token);

        if (u is null)
            throw new UnauthorizedAccessException("Unauthorized");

        DatabasePlaylist pl = new()
        {
            Id = DatabasePlaylist.GenerateId(),
            Name = title,
            Description = description,
            Visibility = visibility,
            VideoIds = [],
            Author = u.UserID,
            LastUpdated = DateTimeOffset.UtcNow
        };

        await PlaylistCollection.InsertOneAsync(pl);
        return pl;
    }

    public async Task AddVideoToPlaylist(string token, string playlistId, InnerTubePlayer video)
    {
        DatabaseUser? u = await DatabaseManager.Users.GetUserFromToken(token);
        DatabasePlaylist? playlist = GetPlaylist(playlistId);

        if (u is null)
            throw new UnauthorizedAccessException("Unauthorized");

        if (playlist is null)
            throw new KeyNotFoundException("Playlist not found");

        if (u.UserID != playlist.Author)
            throw new UnauthorizedAccessException("Unauthorized");

        if (!playlist.VideoIds.Contains(video.Details.Id))
            playlist.VideoIds.Add(video.Details.Id);
        playlist.LastUpdated = DateTimeOffset.UtcNow;

        await PlaylistCollection.ReplaceOneAsync(x => x.Id == playlistId, playlist);
        await DatabaseManager.Cache.AddVideo(new DatabaseVideo
        {
            Id = video.Details.Id,
            Title = video.Details.Title,
            Thumbnails = video.Details.Thumbnails,
            Views = 0,
            Channel = new DatabaseVideoAuthor
            {
                Id = video.Details.Author.Id!,
                Name = video.Details.Author.Title,
                Avatars = []
            },
            Duration = video.Details.Length!.Value.ToDurationString()
        });
    }

    public async Task RemoveVideoFromPlaylist(string token, string playlistId, string videoId)
    {
        DatabaseUser? u = await DatabaseManager.Users.GetUserFromToken(token);
        DatabasePlaylist? playlist = GetPlaylist(playlistId);

        if (u is null)
            throw new UnauthorizedAccessException("Unauthorized");

        if (playlist is null)
            throw new KeyNotFoundException("Playlist not found");

        if (u.UserID != playlist.Author)
            throw new UnauthorizedAccessException("Unauthorized");

        playlist.VideoIds.Remove(videoId);
        playlist.LastUpdated = DateTimeOffset.UtcNow;

        await PlaylistCollection.ReplaceOneAsync(x => x.Id == playlistId, playlist);
    }

    public async Task EditPlaylist(string token, string id, string title, string description, PlaylistVisibility visibility)
    {
        DatabaseUser? u = await DatabaseManager.Users.GetUserFromToken(token);
        DatabasePlaylist? playlist = GetPlaylist(id);

        if (u is null)
            throw new UnauthorizedAccessException("Unauthorized");

        if (playlist is null)
            throw new KeyNotFoundException("Playlist not found");

        if (u.UserID != playlist.Author)
            throw new UnauthorizedAccessException("Unauthorized");

        playlist.Name = title;
        playlist.Description = description;
        playlist.Visibility = visibility;
        playlist.LastUpdated = DateTimeOffset.UtcNow;

        await PlaylistCollection.ReplaceOneAsync(x => x.Id == id, playlist);
    }

    public async Task DeletePlaylist(string token, string id)
    {
        DatabaseUser? u = await DatabaseManager.Users.GetUserFromToken(token);
        DatabasePlaylist? playlist = GetPlaylist(id);

        if (u is null)
            throw new UnauthorizedAccessException("Unauthorized");

        if (playlist is null)
            throw new KeyNotFoundException("Playlist not found");

        if (u.UserID != playlist.Author)
            throw new UnauthorizedAccessException("Unauthorized");

        await PlaylistCollection.DeleteOneAsync(x => x.Id == id);
    }
}