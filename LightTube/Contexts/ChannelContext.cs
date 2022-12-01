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

	public ChannelContext(ChannelTabs tab, InnerTubeChannelResponse channel, string id)
	{
		Id = id;
		CurrentTab = tab;
		Channel = channel;
		Content = channel.Contents;
		Continuation =
			(channel.Contents.FirstOrDefault(x => x is ContinuationItemRenderer) as ContinuationItemRenderer)?.Token;
	}

	public ChannelContext(ChannelTabs tab, InnerTubeContinuationResponse continuation, string id)
	{
		Id = id;
		CurrentTab = tab;
		Content = continuation.Contents;
		Continuation = continuation.Continuation;
	}
}