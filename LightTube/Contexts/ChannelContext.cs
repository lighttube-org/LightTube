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
    public string SubscriberCountText;
    public bool LightTubeAccount;
    public bool Editable;
    public ChannelTabs CurrentTab;

    public IEnumerable<RendererContainer> Content;
    public string Id;
    public string? Continuation;
    public ReadOnlyCollection<ChannelTab> Tabs;

    public ChannelContext(HttpContext context, ChannelTabs tab, InnerTubeChannel channel, string id) : base(context)
    {
        Id = id;
        CurrentTab = tab;
        BannerUrl = channel.Header?.Banner.LastOrDefault()?.Url;
        AvatarUrl = channel.Header?.Avatars.LastOrDefault()?.Url ?? "";
        ChannelTitle = channel.Header?.Title ?? "";
        SubscriberCountText = channel.Header?.SubscriberCountText ?? "";
        LightTubeAccount = false;
        Editable = false;
        Content = channel.Contents;
        Continuation =
            (channel.Contents.FirstOrDefault(x => x.Type == "continuation")?.Data as ContinuationRendererData)
            ?.ContinuationToken;
        Tabs = channel.Tabs;

        AddMeta("description", channel.Metadata.Description);
        AddMeta("author", channel.Metadata.Title);
        AddMeta("og:title", channel.Metadata.Title);
        AddMeta("og:description", channel.Metadata.Description);
        AddMeta("og:url",
            $"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
        AddMeta("og:image", channel.Header?.Avatars.Last().Url ?? "");
        AddMeta("twitter:card", channel.Header?.Avatars.Last().Url ?? "");
        AddRSSUrl($"{context.Request.Scheme}://{context.Request.Host}/channel/{Id}.xml");

        // TODO: most likely broken
        if (channel.Contents.Any(x => x.OriginalType == "channelVideoPlayerRenderer"))
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
        CurrentTab = tab;
        BannerUrl = channel.Header?.Banner.LastOrDefault()?.Url;
        AvatarUrl = channel.Header?.Avatars.Last().Url ?? "";
        ChannelTitle = channel.Header?.Title ?? "";
        SubscriberCountText = channel.Header?.SubscriberCountText ?? "";
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
    }

    public ChannelContext(HttpContext context, DatabaseUser channel, string id) : base(context)
    {
        Id = id;
        CurrentTab = ChannelTabs.Playlists;
        BannerUrl = null;
        AvatarUrl = "";
        ChannelTitle = channel.UserID;
        SubscriberCountText = "LightTube account";
        LightTubeAccount = true;
        Editable = channel.UserID == User?.UserID;
        Tabs = new ReadOnlyCollection<ChannelTab>([
            new ChannelTab(new TabRenderer
            {
                Endpoint = new Endpoint
                {
                    BrowseEndpoint = new()
                    {
                        Params = ""
                    }
                },
                Title = "Playlists",
                Selected = true,
            })
        ]);

        Content = channel.PlaylistRenderers(Localization);
    }
}