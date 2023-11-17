using InnerTube;

namespace LightTube.Contexts;

public class EmbedContext : BaseContext
{
	public PlayerContext Player;
	public InnerTubeNextResponse Video;

	public EmbedContext(HttpContext context, InnerTubePlayer innerTubePlayer, InnerTubeNextResponse innerTubeNextResponse, bool compatibility, SponsorBlockSegment[] sponsors) : base(context)
	{
		Player = new PlayerContext(context, innerTubePlayer, innerTubeNextResponse, "embed", compatibility, context.Request.Query["q"], sponsors);
		Video = innerTubeNextResponse;
	}

	public EmbedContext(HttpContext context, Exception e, InnerTubeNextResponse innerTubeNextResponse) : base(context)
	{
		Player = new PlayerContext(context, e);
		Video = innerTubeNextResponse;
	}
}