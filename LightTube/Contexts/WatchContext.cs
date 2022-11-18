using InnerTube;

namespace LightTube.Contexts;

public class WatchContext : BaseContext
{
	public PlayerContext Player;
	public InnerTubeNextResponse Video;
	public int Dislikes;

	public WatchContext(InnerTubePlayer innerTubePlayer, InnerTubeNextResponse innerTubeNextResponse,
		bool compatibility, int dislikes, HttpContext context) : base()
	{
		Player = new PlayerContext(innerTubePlayer, "embed", compatibility);
		Video = innerTubeNextResponse;
		Dislikes = dislikes;
		GuideHidden = true;

		AddMeta("description", Video.Description);
		AddMeta("author", Video.Channel.Title);
		AddMeta("og:title", Video.Title);
		AddMeta("og:description", Video.Description);
		AddMeta("og:url", $"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
		AddMeta("og:image", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("twitter:card", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("twitter:player", $"https://{context.Request.Host}/embed/${Video.Id}");
		AddMeta("twitter:player:stream", $"https://{context.Request.Host}/proxy/media/${Video.Id}/18");

		AddStylesheet("/lib/videojs/video-js.min.css");
		AddStylesheet("/lib/videojs-endscreen/videojs-endscreen.css");
		AddStylesheet("/lib/silvermine-videojs-quality-selector/silvermine-videojs-quality-selector.css");
		AddStylesheet("/lib/videojs-hls-quality-selector/videojs-hls-quality-selector.css");
		AddStylesheet("/css/vjs-skin.css");

		AddScript("/lib/videojs/video.min.js");
		AddScript("/lib/videojs-hotkeys/videojs.hotkeys.min.js");
		AddScript("/lib/videojs-endscreen/videojs-endscreen.js");
		AddScript("/lib/videojs-contrib-quality-levels/videojs-contrib-quality-levels.min.js");
		AddScript("/lib/videojs-hls-quality-selector/videojs-hls-quality-selector.min.js");
		AddScript("/lib/silvermine-videojs-quality-selector/silvermine-videojs-quality-selector.min.js");
		AddScript("/js/player.js");
	}
}