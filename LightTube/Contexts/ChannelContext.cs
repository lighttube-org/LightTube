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
		BannerUrl = channel.Header?.Banner.Last().Url.ToString() ?? "";
		AvatarUrl = channel.Header?.Avatars.Last().Url.ToString() ?? "";
		ChannelTitle = channel.Header?.Title ?? "";
		SubscriberCountText = channel.Header?.SubscriberCountText ?? "";
		LightTubeAccount = false;
		Editable = false;
		Content = channel.Contents;
		Continuation =
			(channel.Contents.FirstOrDefault(x => x is ContinuationItemRenderer) as ContinuationItemRenderer)?.Token;
		Tabs = channel.EnabledTabs;
	}

	public ChannelContext(HttpContext context, ChannelTabs tab, InnerTubeChannelResponse channel, InnerTubeContinuationResponse continuation, string id) : base(context)
	{
		Id = id;
		CurrentTab = tab;
		BannerUrl = channel.Header?.Banner.Last().Url.ToString() ?? "";
		AvatarUrl = channel.Header?.Avatars.Last().Url.ToString() ?? "";
		ChannelTitle = channel.Header?.Title ?? "";
		SubscriberCountText = channel.Header?.SubscriberCountText ?? "";
		LightTubeAccount = false;
		Editable = false;
		Content = continuation.Contents;
		Continuation = continuation.Continuation;
		Tabs = Enum.GetValues<ChannelTabs>();
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