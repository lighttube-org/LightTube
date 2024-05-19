using InnerTube;
using InnerTube.Models;

namespace LightTube.Contexts;

public class EmbedContext : BaseContext
{
    public PlayerContext Player;
    public InnerTubeVideo Video;

    public EmbedContext(HttpContext context, InnerTubePlayer innerTubePlayer, InnerTubeVideo innerTubeNextResponse,
        bool compatibility, SponsorBlockSegment[] sponsors) : base(context)
    {
        Player = new PlayerContext(context, innerTubePlayer, innerTubeNextResponse, "embed", compatibility,
            context.Request.Query["q"], sponsors);
        Video = innerTubeNextResponse;
    }

    public EmbedContext(HttpContext context, Exception e, InnerTubeVideo innerTubeNextResponse) : base(context)
    {
        Player = new PlayerContext(context, e);
        Video = innerTubeNextResponse;
    }
}