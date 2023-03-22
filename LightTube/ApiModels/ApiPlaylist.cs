using InnerTube;
using InnerTube.Renderers;

namespace LightTube.ApiModels;

public class ApiPlaylist
{
	public string Id { get; }
	public IEnumerable<string> Alerts { get; }
	public string Title { get; }
	public string Description { get; }
	public IEnumerable<Badge> Badges { get; }
	public Channel Channel { get; }
	public IEnumerable<Thumbnail> Thumbnails { get; }
	public string LastUpdated { get; }
	public string VideoCountText { get; }
	public string ViewCountText { get; }
	public string? Continuation { get; }
	public IEnumerable<PlaylistVideoRenderer> Videos { get; }

	public ApiPlaylist(InnerTubePlaylist playlist)
	{
		Id = playlist.Id;
		Alerts = playlist.Alerts;
		Title = playlist.Sidebar.Title;
		Description = playlist.Sidebar.Description;
		Badges = playlist.Sidebar.Badges;
		Channel = playlist.Sidebar.Channel;
		Thumbnails = playlist.Sidebar.Thumbnails;
		LastUpdated = playlist.Sidebar.LastUpdated;
		VideoCountText = playlist.Sidebar.VideoCountText;
		ViewCountText = playlist.Sidebar.ViewCountText;
		Continuation = playlist.Continuation;
		Videos = playlist.Videos;
	}
}