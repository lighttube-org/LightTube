using System.Collections.ObjectModel;
using InnerTube;
using InnerTube.Models;
using InnerTube.Protobuf;
using InnerTube.Renderers;
using LightTube.Database.Models;
using Endpoint = InnerTube.Protobuf.Endpoint;

namespace LightTube.Contexts;

public class ChannelContext : BaseContext
{
	public string? BannerUrl;
	public string AvatarUrl;
	public string ChannelTitle;
	public string? Handle;
	public long SubscriberCount;
	public long VideoCount;
	public string? Tagline;
	public string? PrimaryLink;
	public string? SecondaryLink;
	public bool LightTubeAccount;
	public bool Editable;
	public ChannelTabs CurrentTab;

	public IEnumerable<RendererContainer> Content;
	public string Id;
	public string? Continuation;
	public ReadOnlyCollection<ChannelTab> Tabs;
	public InnerTubeAboutChannel? About;

	public ChannelContext(HttpContext context, ChannelTabs tab, InnerTubeChannel channel, string id, InnerTubeAboutChannel? about = null) : base(context)
	{
		Id = id;
		CurrentTab = channel.Tabs.FirstOrDefault(x => x.Selected)?.Tab ?? tab;
		BannerUrl = channel.Header?.Banner.LastOrDefault()?.Url;
		AvatarUrl = channel.Header?.Avatars.LastOrDefault()?.Url ?? "";
		ChannelTitle = channel.Header?.Title ?? "";
		Handle = channel.Header?.Handle;
		SubscriberCount = channel.Header?.SubscriberCount ?? 0;
		VideoCount = channel.Header?.VideoCount ?? 0;
		Tagline = channel.Header?.Tagline;
		PrimaryLink = channel.Header?.PrimaryLink;
		SecondaryLink = channel.Header?.SecondaryLink;
		LightTubeAccount = false;
		Editable = false;
		Content = channel.Contents;
		Continuation =
			(channel.Contents.FirstOrDefault(x => x.Type == "continuation")?.Data as ContinuationRendererData)
			?.ContinuationToken;
		Tabs = channel.Tabs;
		About = about;

		AddMeta("description", channel.Metadata.Description);
		AddMeta("author", channel.Metadata.Title);
		AddMeta("og:title", channel.Metadata.Title);
		AddMeta("og:description", channel.Metadata.Description);
		AddMeta("og:url",
			$"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
		AddMeta("og:image", channel.Header?.Avatars.Last().Url ?? "");
		AddMeta("twitter:card", channel.Header?.Avatars.Last().Url ?? "");
		AddRSSUrl($"{context.Request.Scheme}://{context.Request.Host}/channel/{Id}.xml");

		if (channel.Contents
		    .Select(x => x.OriginalType == "itemSectionRenderer" ? (x.Data as ContainerRendererData)!.Items.First() : x)
		    .Any(x => x.OriginalType == "channelVideoPlayerRenderer"))
		{
			AddStylesheet("/lib/ltplayer.css");
			AddScript("/lib/ltplayer.js");
			AddScript("/js/player.js");
		}
	}

	public ChannelContext(HttpContext context, ChannelTabs tab, InnerTubeChannel channel,
		ContinuationResponse continuation, string id) : base(context)
	{
		Id = id;
		CurrentTab = channel.Tabs.FirstOrDefault(x => x.Selected)?.Tab ?? tab;
		BannerUrl = channel.Header?.Banner.LastOrDefault()?.Url;
		AvatarUrl = channel.Header?.Avatars.Last().Url ?? "";
		ChannelTitle = channel.Header?.Title ?? "";
		Handle = channel.Header?.Handle;
		SubscriberCount = channel.Header?.SubscriberCount ?? 0;
		VideoCount = channel.Header?.VideoCount ?? 0;
		Tagline = channel.Header?.Tagline;
		PrimaryLink = channel.Header?.PrimaryLink;
		SecondaryLink = channel.Header?.SecondaryLink;
		LightTubeAccount = false;
		Editable = false;
		Content = continuation.Results;
		Continuation = continuation.ContinuationToken;
		Tabs = channel.Tabs;

		AddMeta("description", channel.Metadata.Description);
		AddMeta("author", channel.Metadata.Title);
		AddMeta("og:title", channel.Metadata.Title);
		AddMeta("og:description", channel.Metadata.Description);
		AddMeta("og:url",
			$"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
		AddMeta("og:image", channel.Header?.Avatars.Last().Url ?? "");
		AddMeta("twitter:card", channel.Header?.Avatars.Last().Url ?? "");
		AddRSSUrl(context.Request.Scheme + "://" + context.Request.Host + "/feed/" + Id + "/rss.xml");
	
		if (channel.Contents
		    .Select(x => x.OriginalType == "itemSectionRenderer" ? (x.Data as ContainerRendererData)!.Items.First() : x)
		    .Any(x => x.OriginalType == "channelVideoPlayerRenderer"))
		{
			AddStylesheet("/lib/ltplayer.css");
			AddScript("/lib/ltplayer.js");
			AddScript("/js/player.js");
		}
	}

	public ChannelContext(HttpContext context, DatabaseUser channel, string id) : base(context)
	{
		Id = id;
		CurrentTab = ChannelTabs.Playlists;
		BannerUrl = null;
		AvatarUrl = "";
		ChannelTitle = channel.UserId;
		Handle = "@LT_" + id;
		SubscriberCount = 0;
		VideoCount = 0;
		Tagline = Localization.GetRawString("channel.tagline.lighttube");
		PrimaryLink = null;
		SecondaryLink = null;
		LightTubeAccount = true;
		Editable = channel.UserId == User?.UserId;
		Tabs = new ReadOnlyCollection<ChannelTab>([
			new ChannelTab(new TabRenderer
			{
				Endpoint = new Endpoint
				{
					BrowseEndpoint = new()
					{
						Params = "EglwbGF5bGlzdHP"
					}
				},
				Title = "Playlists",
				Selected = true,
				
			})
		]);

		Content = channel.PlaylistRenderers(Localization);
	}
}