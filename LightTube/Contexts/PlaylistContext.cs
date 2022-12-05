using InnerTube;
using InnerTube.Renderers;

namespace LightTube.Contexts;

public class PlaylistContext : BaseContext
{
	public InnerTubePlaylist Playlist;
	public IEnumerable<IRenderer> Items;
	public string? Continuation;

	public PlaylistContext(HttpContext context, InnerTubePlaylist playlist) : base(context)
	{
		Playlist = playlist;
		Items = playlist.Videos;
		Continuation = playlist.Continuation;
	}

	public PlaylistContext(HttpContext context, InnerTubePlaylist playlist, InnerTubeContinuationResponse continuation) : base(context)
	{
		Playlist = playlist;
		Items = continuation.Contents;
		Continuation = continuation.Continuation;
	}
}