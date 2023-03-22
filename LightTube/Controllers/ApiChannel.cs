using InnerTube;
using InnerTube.Renderers;

namespace LightTube.Controllers;

public class ApiChannel
{
	public string Id { get; }
	public string Title { get; }
	public IEnumerable<Thumbnail> Avatars { get; }
	public IEnumerable<Thumbnail> Banner { get; }
	public IEnumerable<Badge> Badges { get; }
	public IEnumerable<ChannelLink> PrimaryLinks { get; }
	public IEnumerable<ChannelLink> SecondaryLinks { get; }
	public string SubscriberCountText { get; }
	public IEnumerable<string> EnabledTabs { get; }
	public IEnumerable<IRenderer> Contents { get; }
	public string? Continuation { get; }

	public ApiChannel(InnerTubeChannelResponse channel)
	{
		if (channel.Header != null)
		{
			Id = channel.Header.Id;
			Title = channel.Header.Title;
			Avatars = channel.Header.Avatars;
			Banner = channel.Header.Banner;
			Badges = channel.Header.Badges;
			PrimaryLinks = channel.Header.PrimaryLinks;
			SecondaryLinks = channel.Header.SecondaryLinks;
			SubscriberCountText = channel.Header.SubscriberCountText;
		}
		else
		{
			Id = channel.Metadata.Id;
			Title = channel.Metadata.Title;
			Avatars = channel.Metadata.Avatar;
			Banner = Array.Empty<Thumbnail>();
			Badges = Array.Empty<Badge>();
			PrimaryLinks = Array.Empty<ChannelLink>();
			SecondaryLinks = Array.Empty<ChannelLink>();
			SubscriberCountText = "Unavailable";
		}

		EnabledTabs = channel.EnabledTabs.Select(x => x.ToString());
		Contents = channel.Contents;
		Continuation =
			(channel.Contents.FirstOrDefault(x => x is ContinuationItemRenderer) as ContinuationItemRenderer)?.Token;
	}

	public ApiChannel(InnerTubeContinuationResponse channel)
	{
		Id = "";
		Title = "";
		Avatars = Array.Empty<Thumbnail>();
		Banner = Array.Empty<Thumbnail>();
		Badges = Array.Empty<Badge>();
		PrimaryLinks = Array.Empty<ChannelLink>();
		SecondaryLinks = Array.Empty<ChannelLink>();
		SubscriberCountText = "Unavailable";
		EnabledTabs = Array.Empty<string>();
		Contents = channel.Contents;
		Continuation = channel.Continuation;
	}
}