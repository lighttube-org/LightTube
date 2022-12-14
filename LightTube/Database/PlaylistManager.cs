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

	public IEnumerable<PlaylistVideoRenderer> GetPlaylistVideos(string id)
	{
		DatabasePlaylist? pl = GetPlaylist(id);
		if (pl == null) return Array.Empty<PlaylistVideoRenderer>();

		List<PlaylistVideoRenderer> renderers = new();

		for (int i = 0; i < pl.VideoIds.Count; i++)
		{
			string videoId = pl.VideoIds[i];
			DatabaseVideo? video = VideoCacheCollection.FindSync(x => x.Id == videoId).FirstOrDefault();
			string json = INNERTUBE_PLAYLIST_VIDEO_RENDERER_TEMPLATE
				.Replace("%%ID%%", videoId)
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
}