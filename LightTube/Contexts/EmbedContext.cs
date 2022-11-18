using InnerTube;

namespace LightTube.Contexts;

public class EmbedContext : BaseContext
{
	public PlayerContext Player;
	public InnerTubeNextResponse Video;

	public EmbedContext(InnerTubePlayer innerTubePlayer, InnerTubeNextResponse innerTubeNextResponse, bool compatibility)
	{
		Player = new PlayerContext(innerTubePlayer, "embed", compatibility);
		Video = innerTubeNextResponse;
	}

	public EmbedContext(Exception e, InnerTubeNextResponse innerTubeNextResponse)
	{
		Player = new PlayerContext(e);
		Video = innerTubeNextResponse;
	}
}