using InnerTube;
using InnerTube.Renderers;
using LightTube.Database;
using LightTube.Database.Models;

namespace LightTube.Contexts;

public class PlaylistContext : BaseContext
{
	public string PlaylistThumbnail;
	public string PlaylistTitle;
	public string AuthorName;
	public string AuthorId;
	public string ViewCountText;
	public string LastUpdatedText;
	public bool Editable;
	public IEnumerable<IRenderer> Items;
	public string? Continuation;

	public PlaylistContext(HttpContext context, InnerTubePlaylist playlist) : base(context)
	{
		PlaylistThumbnail = playlist.Sidebar.Thumbnails.Last().Url.ToString();
		PlaylistTitle = playlist.Sidebar.Title;
		AuthorName = playlist.Sidebar.Channel.Title;
		AuthorId = playlist.Sidebar.Channel.Id!;
		ViewCountText = playlist.Sidebar.ViewCountText;
		LastUpdatedText = playlist.Sidebar.LastUpdated;
		Editable = false;
		Items = playlist.Videos;
		Continuation = playlist.Continuation;
	}

	public PlaylistContext(HttpContext context, InnerTubePlaylist playlist, InnerTubeContinuationResponse continuation) : base(context)
	{
		PlaylistThumbnail = playlist.Sidebar.Thumbnails.Last().Url.ToString();
		PlaylistTitle = playlist.Sidebar.Title;
		AuthorName = playlist.Sidebar.Channel.Title;
		AuthorId = playlist.Sidebar.Channel.Id!;
		ViewCountText = playlist.Sidebar.ViewCountText;
		LastUpdatedText = playlist.Sidebar.LastUpdated;
		Editable = false;
		Items = continuation.Contents;
		Continuation = continuation.Continuation;
	}

	public PlaylistContext(HttpContext context, DatabasePlaylist? playlist) : base(context)
	{
		bool visible = (playlist?.Visibility == PlaylistVisibility.PRIVATE)
			? User != null && User.UserID == playlist.Author
			: true;
		
		if (visible && playlist != null)
		{
			PlaylistThumbnail = $"https://i.ytimg.com/vi/{playlist.VideoIds.First()}/hqdefault.jpg";
			PlaylistTitle = playlist.Name;
			AuthorName = playlist.Author;
			AuthorId = DatabaseManager.Users.GetUserFromId(playlist.Author).Result?.LTChannelID ?? "";
			ViewCountText = "LightTube playlist";
			LastUpdatedText = $"Last updated on {playlist.LastUpdated:MMM d, yyyy}";
			Items = DatabaseManager.Playlists.GetPlaylistVideos(playlist.Id);
			Editable = User != null && User.UserID == playlist.Author;
		}
		else
		{
			PlaylistThumbnail = $"https://i.ytimg.com/vi//hqdefault.jpg";
			PlaylistTitle = "Playlist unavailable";
			AuthorName = "";
			AuthorId = "";
			ViewCountText = "LightTube playlist";
			LastUpdatedText = "";
			Items = Array.Empty<IRenderer>();
			Editable = false;
		}
	}
}