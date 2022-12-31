using InnerTube;
using InnerTube.Renderers;
using LightTube.Database.Models;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace LightTube.Database;

public class PlaylistManager
{
	private const string INNERTUBE_PLAYLIST_VIDEO_RENDERER_TEMPLATE = "{\"videoId\":\"%%ID%%\",\"isPlayable\":true,\"thumbnail\":{\"thumbnails\":[{\"url\":\"%%THUMBNAIL%%\",\"width\":0,\"height\":0}]},\"title\":{\"runs\":[{\"text\":\"%%TITLE%%\"}]},\"index\":{\"simpleText\":\"%%INDEX%%\"},\"shortBylineText\":{\"runs\":[{\"text\":\"%%CHANNEL_TITLE%%\",\"navigationEndpoint\":{\"browseEndpoint\":{\"browseId\":\"%%CHANNEL_ID%%\"}}}]},\"lengthText\":{\"simpleText\":\"%%DURATION%%\"},\"navigationEndpoint\":{\"watchEndpoint\":{\"videoId\":\"%%ID%%\"}},\"lengthSeconds\":\"%%DURATION_SECONDS%%\",\"isPlayable\":true,\"thumbnailOverlays\":[{\"thumbnailOverlayTimeStatusRenderer\":{\"text\":{\"simpleText\":\"%%DURATION%%\"}}}],\"videoInfo\":{\"runs\":[{\"text\":\"%%VIEWS%%\"},{\"text\":\" • \"},{\"text\":\"%%UPLOADED_AT%%\"}]}}";
	private const string INNERTUBE_PLAYLIST_PANEL_VIDEO_RENDERER_TEMPLATE = "{\"title\":{\"simpleText\":\"%%TITLE%%\"},\"thumbnail\":{\"thumbnails\":[{\"url\":\"%%THUMBNAIL%%\",\"width\":0,\"height\":0}]},\"lengthText\":{\"simpleText\":\"%%DURATION%%\"},\"indexText\":{\"simpleText\":\"%%INDEX%%\"},\"selected\":%%SELECTED%%,\"navigationEndpoint\":{\"watchEndpoint\":{\"params\":\"OAE%3D\"}},\"videoId\":\"%%ID%%\",\"shortBylineText\":{\"runs\":[{\"text\":\"%%CHANNEL_TITLE%%\",\"navigationEndpoint\":{\"browseEndpoint\":{\"browseId\":\"%%CHANNEL_ID%%\"}}}]}}";
	public IMongoCollection<DatabasePlaylist> PlaylistCollection { get; }
	public IMongoCollection<DatabaseVideo> VideoCacheCollection { get; }

	public PlaylistManager(
		IMongoCollection<DatabasePlaylist> playlistCollection,
		IMongoCollection<DatabaseVideo> videoCacheCollection)
	{
		PlaylistCollection = playlistCollection;
		VideoCacheCollection = videoCacheCollection;
	}

	public DatabasePlaylist? GetPlaylist(string id) => PlaylistCollection.FindSync(x => x.Id == id).FirstOrDefault();

	public IEnumerable<DatabasePlaylist> GetUserPlaylists(string userId, PlaylistVisibility minVisibility)
	{
		IAsyncCursor<DatabasePlaylist> unfiltered = PlaylistCollection.FindSync(x => x.Author == userId);
		return unfiltered.ToList().Where(x => x.Visibility >= minVisibility);
	}

	public IEnumerable<PlaylistVideoRenderer> GetPlaylistVideos(string id, bool editable)
	{
		DatabasePlaylist? pl = GetPlaylist(id);
		if (pl == null) return Array.Empty<PlaylistVideoRenderer>();

		List<PlaylistVideoRenderer> renderers = new();

		for (int i = 0; i < pl.VideoIds.Count; i++)
		{
			string videoId = pl.VideoIds[i];
			DatabaseVideo? video = VideoCacheCollection.FindSync(x => x.Id == videoId).FirstOrDefault();
			string json = INNERTUBE_PLAYLIST_VIDEO_RENDERER_TEMPLATE
				.Replace("%%ID%%", editable ? videoId + "!": videoId)
				.Replace("%%INDEX%%", (i + 1).ToString())
				.Replace("%%TITLE%%", video?.Title ?? "Uncached video. Click to fix")
				.Replace("%%THUMBNAIL%%", video?.Thumbnails.LastOrDefault()?.Url.ToString() ?? "https://i.ytimg.com/vi//hqdefault.jpg")
				.Replace("%%DURATION%%", video?.Duration ?? "00:00")
				.Replace("%%DURATION_SECONDS%%", InnerTube.Utils.ParseDuration(video?.Duration ?? "00:00").TotalSeconds.ToString())
				.Replace("%%UPLADED_AT%%", video?.UploadedAt ?? "???")
				.Replace("%%CHANNEL_TITLE%%", video?.Channel.Name ?? "???")
				.Replace("%%CHANNEL_ID%%", video?.Channel.Id ?? "???")
				.Replace("%%VIEWS%%", (video?.Views ?? 0).ToString());
			renderers.Add(new PlaylistVideoRenderer(JObject.Parse(json)));
		}

		return renderers;
	}

	public IEnumerable<PlaylistPanelVideoRenderer> GetPlaylistPanelVideos(string id, string currentVideoId)
	{
		DatabasePlaylist? pl = GetPlaylist(id);
		if (pl == null) return Array.Empty<PlaylistPanelVideoRenderer>();

		List<PlaylistPanelVideoRenderer> renderers = new();

		for (int i = 0; i < pl.VideoIds.Count; i++)
		{
			string videoId = pl.VideoIds[i];
			DatabaseVideo? video = VideoCacheCollection.FindSync(x => x.Id == videoId).FirstOrDefault();
			string json = INNERTUBE_PLAYLIST_PANEL_VIDEO_RENDERER_TEMPLATE
				.Replace("%%ID%%", videoId)
				.Replace("%%SELECTED%%", (currentVideoId == videoId).ToString().ToLower())
				.Replace("%%INDEX%%", currentVideoId == videoId ? ">" : (i + 1).ToString())
				.Replace("%%TITLE%%", video?.Title ?? "Uncached video. Click to fix")
				.Replace("%%THUMBNAIL%%", video?.Thumbnails.LastOrDefault()?.Url.ToString() ?? "https://i.ytimg.com/vi//hqdefault.jpg")
				.Replace("%%DURATION%%", video?.Duration ?? "00:00")
				.Replace("%%CHANNEL_TITLE%%", video?.Channel.Name ?? "???")
				.Replace("%%CHANNEL_ID%%", video?.Channel.Id ?? "???");
			renderers.Add(new PlaylistPanelVideoRenderer(JObject.Parse(json)));
		}

		return renderers;
	}

	public string GetPlaylistPanelVideosJson(string id, string currentVideoId)
	{
		DatabasePlaylist? pl = GetPlaylist(id);
		if (pl == null) return "";

		List<string> renderers = new();

		for (int i = 0; i < pl.VideoIds.Count; i++)
		{
			string videoId = pl.VideoIds[i];
			DatabaseVideo? video = VideoCacheCollection.FindSync(x => x.Id == videoId).FirstOrDefault();
			string json = $"{{\"playlistPanelVideoRenderer\":{INNERTUBE_PLAYLIST_PANEL_VIDEO_RENDERER_TEMPLATE}}}"
				.Replace("%%ID%%", videoId)
				.Replace("%%SELECTED%%", (currentVideoId == videoId).ToString().ToLower())
				.Replace("%%INDEX%%", currentVideoId == videoId ? "▶" : (i + 1).ToString())
				.Replace("%%TITLE%%", video?.Title ?? "Uncached video. Click to fix")
				.Replace("%%THUMBNAIL%%", video?.Thumbnails.LastOrDefault()?.Url.ToString() ?? "https://i.ytimg.com/vi//hqdefault.jpg")
				.Replace("%%DURATION%%", video?.Duration ?? "00:00")
				.Replace("%%CHANNEL_TITLE%%", video?.Channel.Name ?? "???")
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

		DatabasePlaylist pl = new DatabasePlaylist()
		{
			Id = DatabasePlaylist.GenerateId(),
			Name = title,
			Description = description,
			Visibility = visibility,
			VideoIds = new(),
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
		await DatabaseManager.Cache.AddVideo(new DatabaseVideo()
		{
			Id = video.Details.Id,
			Title = video.Details.Title,
			Thumbnails = new Thumbnail[] {
				new Thumbnail()
				{
					Url = new Uri($"https://i.ytimg.com/vi/{video.Details.Id}/hqdefault.jpg")
				}
			},
			Views = video.Details.ViewCount,
			Channel = new()
			{
				Id = video.Details.Author.Id!,
				Name = video.Details.Author.Title,
				Avatars = new Thumbnail[] {
				new Thumbnail()
				{
					Url = video.Details.Author.Avatar!
				}
			}
			},
			Duration = video.Details.Length.ToDurationString()
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