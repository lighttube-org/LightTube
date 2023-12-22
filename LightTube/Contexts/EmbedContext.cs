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
		
		AddMeta("description", Video.Description);
		AddMeta("author", Video.Channel.Title);
		AddMeta("og:title", Video.Title);
		AddMeta("og:description", Video.Description);
		AddMeta("og:url",
			$"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
		AddMeta("og:image", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("og:video:url", $"https://{context.Request.Host}/proxy/media/{Video.Id}/18.mp4");
		AddMeta("og:video:width", "640");
		AddMeta("og:video:height", "360");
		AddMeta("og:type", "video.other");
		AddMeta("twitter:card", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("twitter:player", $"https://{context.Request.Host}/embed/{Video.Id}");
		AddMeta("twitter:player:stream", $"https://{context.Request.Host}/proxy/media/{Video.Id}/18.mp4");
	}

	public EmbedContext(HttpContext context, Exception e, InnerTubeNextResponse innerTubeNextResponse) : base(context)
	{
		Player = new PlayerContext(context, e);
		Video = innerTubeNextResponse;
		
		AddMeta("description", Video.Description);
		AddMeta("author", Video.Channel.Title);
		AddMeta("og:title", Video.Title);
		AddMeta("og:description", Video.Description);
		AddMeta("og:url",
			$"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
		AddMeta("og:image", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("og:video:url", $"https://{context.Request.Host}/proxy/media/{Video.Id}/18.mp4");
		AddMeta("og:video:width", "640");
		AddMeta("og:video:height", "360");
		AddMeta("og:type", "video.other");
		AddMeta("twitter:card", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("twitter:player", $"https://{context.Request.Host}/embed/{Video.Id}");
		AddMeta("twitter:player:stream", $"https://{context.Request.Host}/proxy/media/{Video.Id}/18.mp4");
	}
}