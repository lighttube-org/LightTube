namespace LightTube.Database.Models;

public class DatabasePlaylist
{
	public string Id;
	public string Name;
	public string Description;
	public PlaylistVisibility Visibility;
	public List<string> VideoIds;
	public string Author;
	public DateTimeOffset LastUpdated;
}

public enum PlaylistVisibility
{
	PRIVATE,
	UNLISTED,
	VISIBLE
}