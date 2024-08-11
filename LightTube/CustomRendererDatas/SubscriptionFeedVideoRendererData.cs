using InnerTube.Renderers;

namespace LightTube.CustomRendererDatas;

public class SubscriptionFeedVideoRendererData : PlaylistVideoRendererData
{
	public DateTimeOffset ExactPublishDate { get; set; }
}