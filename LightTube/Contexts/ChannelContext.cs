using InnerTube;
using InnerTube.Renderers;
using LightTube.Database.Models;

namespace LightTube.Contexts;

public class ChannelContext : BaseContext
{
	public string? BannerUrl;
	public string AvatarUrl;
	public string ChannelTitle;
	public string SubscriberCountText;
	public bool LightTubeAccount;
	public bool Editable;
	public ChannelTabs CurrentTab;

	[Obsolete]
	public InnerTubeChannelResponse? Channel;
	public IEnumerable<IRenderer> Content;
	public string Id;
	public string? Continuation;
	public ChannelTabs[] Tabs;

	public ChannelContext(HttpContext context, ChannelTabs tab, InnerTubeChannelResponse channel, string id) : base(context)
	{
		Id = id;
		CurrentTab = tab;
		BannerUrl = channel.Header?.Banner.LastOrDefault()?.Url.ToString();
		AvatarUrl = channel.Header?.Avatars.LastOrDefault()?.Url.ToString() ?? "";
		ChannelTitle = channel.Header?.Title ?? "";
		SubscriberCountText = channel.Header?.SubscriberCountText ?? "";
		LightTubeAccount = false;
		Editable = false;
		Content = channel.Contents;
		Continuation =
			(channel.Contents.FirstOrDefault(x => x is ContinuationItemRenderer) as ContinuationItemRenderer)?.Token;
		Tabs = channel.EnabledTabs;

		AddMeta("description", channel.Metadata.Description);
		AddMeta("author", channel.Metadata.Title);
		AddMeta("og:title", channel.Metadata.Title);
		AddMeta("og:description", channel.Metadata.Description);
		AddMeta("og:url", $"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
		AddMeta("og:image", channel.Header?.Avatars.Last().Url.ToString() ?? "");
		AddMeta("twitter:card", channel.Header?.Avatars.Last().Url.ToString() ?? "");

		if (channel.Contents.Any(x => x is ChannelVideoPlayerRenderer || x is ItemSectionRenderer isr && isr.Contents.Any(y => y is ChannelVideoPlayerRenderer)))
		{
			AddStylesheet("/lib/videojs/video-js.min.css");
			AddStylesheet("/lib/videojs-endscreen/videojs-endscreen.css");
			AddStylesheet("/lib/videojs-vtt-thumbnails/videojs-vtt-thumbnails.min.css");
			AddStylesheet("/lib/videojs-hls-quality-selector/videojs-hls-quality-selector.css");
			AddStylesheet("/lib/silvermine-videojs-quality-selector/silvermine-videojs-quality-selector.css");
			AddStylesheet("/css/vjs-skin.css");

			AddScript("/lib/videojs/video.min.js");
			AddScript("/lib/videojs-hotkeys/videojs.hotkeys.min.js");
			AddScript("/lib/videojs-endscreen/videojs-endscreen.js");
			AddScript("/lib/videojs-vtt-thumbnails/videojs-vtt-thumbnails.min.js");
			AddScript("/lib/videojs-contrib-quality-levels/videojs-contrib-quality-levels.min.js");
			AddScript("/lib/videojs-hls-quality-selector/videojs-hls-quality-selector.min.js");
			AddScript("/lib/silvermine-videojs-quality-selector/silvermine-videojs-quality-selector.min.js");
			AddScript("/js/player.js");
		}
	}

	public ChannelContext(HttpContext context, ChannelTabs tab, InnerTubeChannelResponse channel, InnerTubeContinuationResponse continuation, string id) : base(context)
	{
		Id = id;
		CurrentTab = tab;
		BannerUrl = channel.Header?.Banner.LastOrDefault()?.Url.ToString();
		AvatarUrl = channel.Header?.Avatars.Last().Url.ToString() ?? "";
		ChannelTitle = channel.Header?.Title ?? "";
		SubscriberCountText = channel.Header?.SubscriberCountText ?? "";
		LightTubeAccount = false;
		Editable = false;
		Content = continuation.Contents;
		Continuation = continuation.Continuation;
		Tabs = Enum.GetValues<ChannelTabs>();

		AddMeta("description", channel.Metadata.Description);
		AddMeta("author", channel.Metadata.Title);
		AddMeta("og:title", channel.Metadata.Title);
		AddMeta("og:description", channel.Metadata.Description);
		AddMeta("og:url", $"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
		AddMeta("og:image", channel.Header?.Avatars.Last().Url.ToString() ?? "");
		AddMeta("twitter:card", channel.Header?.Avatars.Last().Url.ToString() ?? "");
	}

	public ChannelContext(HttpContext context, DatabaseUser? channel, string id) : base(context)
	{
		Id = id;
		CurrentTab = ChannelTabs.Playlists;
		BannerUrl = null;
		AvatarUrl = "";
		ChannelTitle = channel?.UserID ?? "";
		SubscriberCountText = "LightTube account";
		LightTubeAccount = true;
		Editable = channel?.UserID == User?.UserID;
		Tabs = new[] {
			ChannelTabs.Playlists
		};

		Content = new IRenderer[1] {
			channel?.PlaylistRenderers()
		};
	}
}