using InnerTube;
using InnerTube.Models;
using InnerTube.Protobuf;
using InnerTube.Renderers;
using LightTube.Database;
using LightTube.Database.Models;
using LightTube.Localization;
using Endpoint = InnerTube.Protobuf.Endpoint;

namespace LightTube.ApiModels;

public class ApiPlaylist
{
	public string Id { get; }
	public string[] Alerts { get; }
	public RendererContainer[] Contents { get; }
	public RendererContainer[] Chips { get; }
	public string? Continuation { get; }
	public PlaylistSidebar? Sidebar { get; }
	public bool Editable { get; }

	public ApiPlaylist(InnerTubePlaylist playlist)
	{
		Id = playlist.Id;
		Alerts = playlist.Alerts;
		Contents = playlist.Contents;
		Chips = playlist.Chips;
		Continuation = playlist.Continuation;
		Sidebar = playlist.Sidebar;
		Editable = false;
	}

	public ApiPlaylist(ContinuationResponse playlist)
	{
		Id = "";
		Alerts = [];
		Contents = playlist.Results;
		Chips = [];
		Continuation = playlist.ContinuationToken;
		Sidebar = null;
		Editable = false;
	}

	public ApiPlaylist(DatabasePlaylist playlist, DatabaseUser author, LocalizationManager localization, DatabaseUser? user)
	{
		Id = playlist.Id;
		Alerts = [];
		Contents = DatabaseManager.Playlists.GetPlaylistVideoRenderers(playlist.Id, playlist.Author == user?.UserID, localization).ToArray();
		Chips = [];
		Continuation = null;
		Sidebar = new PlaylistSidebar(playlist.GetHeaderRenderer(author, localization), "en");
		Editable = playlist.Author == user?.UserID;
	}
}