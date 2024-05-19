using InnerTube;
using InnerTube.Models;
using InnerTube.Protobuf;
using InnerTube.Renderers;
using LightTube.Database;
using LightTube.Database.Models;
using LightTube.Localization;

namespace LightTube.ApiModels;

public class ApiPlaylist
{
	public string Id { get; }
	public string[] Alerts { get; }
	public RendererContainer[] Contents { get; }
	public RendererContainer[] Chips { get; }
	public string? Continuation { get; }
	public PlaylistSidebar? Sidebar { get; }

	public ApiPlaylist(InnerTubePlaylist playlist)
	{
		Id = playlist.Id;
		Alerts = playlist.Alerts;
		Contents = playlist.Contents;
		Chips = playlist.Chips;
		Continuation = playlist.Continuation;
		Sidebar = playlist.Sidebar;
	}

	public ApiPlaylist(ContinuationResponse playlist)
	{
		Id = "";
		Alerts = [];
		Contents = playlist.Results;
		Chips = [];
		Continuation = playlist.ContinuationToken;
		Sidebar = null;
	}

	public ApiPlaylist(DatabasePlaylist playlist, LocalizationManager localization)
	{
		Id = playlist.Id;
		Alerts = [];
		Contents = DatabaseManager.Playlists.GetPlaylistVideos(playlist.Id, false, localization).ToArray();
		Chips = [];
		Continuation = null;
		Sidebar = null; // TODO
		/*
		Title = playlist.Name;
		Description = playlist.Description;
		Badges = [];
		DatabaseUser user = DatabaseManager.Users.GetUserFromId(playlist.Author).Result!;
		Channel = new Channel
		{
			Id = user.LTChannelID,
			Title = user.UserID,
			Avatar = null,
			Subscribers = null,
			Badges = []
		};
		Thumbnails =
		[
			new Thumbnail
			{
				Width = null,
				Height = null,
				Url = new Uri($"https://i.ytimg.com/vi/{playlist.VideoIds.FirstOrDefault()}/hqdefault.jpg")
			}
		];
		LastUpdated = $"Last updated on {playlist.LastUpdated:MMM d, yyyy}";
		VideoCountText = playlist.VideoIds.Count switch
		{
			0 => "No videos",
			1 => "1 video",
			_ => $"{playlist.VideoIds.Count} videos"
		};
		ViewCountText = "LightTube playlist";
		Continuation = null;
		Videos = 
		*/
	}
}