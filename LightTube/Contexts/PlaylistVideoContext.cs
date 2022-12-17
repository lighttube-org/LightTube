using InnerTube;
using LightTube.Database.Models;

namespace LightTube.Contexts;

public class PlaylistVideoContext : ModalContext
{
	public string ItemId;
	public string ItemTitle;
	public string ItemSubtitle;
	public string ItemThumbnail;
	public IEnumerable<DatabasePlaylist>? Playlists;

	public PlaylistVideoContext(HttpContext context) : base(context)
	{
	}

	public PlaylistVideoContext(HttpContext context, InnerTubeNextResponse video) : base(context)
	{
		ItemId = video.Id;
		ItemTitle = video.Title;
		ItemSubtitle = video.Channel.Title;
		ItemThumbnail = $"https://i.ytimg.com/vi/{video.Id}/hqdefault.jpg";
	}

	public PlaylistVideoContext(HttpContext context, DatabaseVideo? video, string id = "") : base(context)
	{
		ItemId = (video?.Id ?? id)[..11];
		ItemTitle = video?.Title ?? "Uncached Video";
		ItemSubtitle = video?.Channel.Name ?? "";
		ItemThumbnail = $"https://i.ytimg.com/vi/{video?.Id}/hqdefault.jpg";
	}

	public PlaylistVideoContext(HttpContext context, DatabasePlaylist playlist) : base(context)
	{
		ItemId = playlist.Id;
		ItemTitle = playlist.Name;
		ItemSubtitle = $"{playlist.VideoIds.Count} videos";
		ItemThumbnail = $"https://i.ytimg.com/vi/{playlist.VideoIds.FirstOrDefault()}/hqdefault.jpg";
	}
}