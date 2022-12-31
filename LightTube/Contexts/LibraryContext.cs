using LightTube.Database;
using LightTube.Database.Models;

namespace LightTube.Contexts;

public class LibraryContext : BaseContext
{
	public IEnumerable<DatabasePlaylist> Playlists;

	public LibraryContext(HttpContext context) : base(context)
	{
		Playlists = User != null
			? DatabaseManager.Playlists.GetUserPlaylists(User.UserID, PlaylistVisibility.PRIVATE)
			: Array.Empty<DatabasePlaylist>();
	}
}