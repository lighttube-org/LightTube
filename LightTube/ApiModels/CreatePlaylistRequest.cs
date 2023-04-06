using LightTube.Database.Models;

namespace LightTube.Controllers;

public class CreatePlaylistRequest
{
	public string Title;
	public string? Description;
	public PlaylistVisibility? Visibility;
}