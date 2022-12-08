using InnerTube;
using InnerTube.Renderers;

namespace LightTube.Contexts;

public class ChannelContext : BaseContext
{
	public ChannelTabs CurrentTab;
	public InnerTubeChannelResponse? Channel;
	public IEnumerable<IRenderer> Content;
	public string Id;
	public string? Continuation;
	public ChannelTabs[] Tabs;

	public ChannelContext(HttpContext context, ChannelTabs tab, InnerTubeChannelResponse channel, string id) : base(context)
	{
		Id = id;
		CurrentTab = tab;
		Channel = channel;
		Content = channel.Contents;
		Continuation =
			(channel.Contents.FirstOrDefault(x => x is ContinuationItemRenderer) as ContinuationItemRenderer)?.Token;
		Tabs = channel.EnabledTabs;
	}

	public ChannelContext(HttpContext context, ChannelTabs tab, InnerTubeContinuationResponse continuation, string id) : base(context)
	{
		Id = id;
		CurrentTab = tab;
		Content = continuation.Contents;
		Continuation = continuation.Continuation;
		Tabs = Enum.GetValues<ChannelTabs>();
	}
}