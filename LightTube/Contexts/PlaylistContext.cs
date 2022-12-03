using InnerTube;
using InnerTube.Renderers;

namespace LightTube.Contexts;

public class PlaylistContext : BaseContext
{
	public InnerTubePlaylist Playlist;
	public IEnumerable<IRenderer> Items;
	public string? Continuation;

	public PlaylistContext(InnerTubePlaylist playlist)
	{
		Playlist = playlist;
		Items = playlist.Videos;
		Continuation = playlist.Continuation;
	}

	public PlaylistContext(InnerTubePlaylist playlist, InnerTubeContinuationResponse continuation)
	{
		Playlist = playlist;
		Items = continuation.Contents;
		Continuation = continuation.Continuation;
	}
}