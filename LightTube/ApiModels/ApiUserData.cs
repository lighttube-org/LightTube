using InnerTube.Renderers;
using LightTube.Database.Models;

namespace LightTube.ApiModels;

public class ApiUserData
{
    public DatabaseUser User { get; private set; }
    public Dictionary<string, ApiSubscriptionInfo> Channels { get; private set; }

    public static ApiUserData? GetFromDatabaseUser(DatabaseUser? user)
    {
        if (user is null) return null;
        return new ApiUserData
        {
            User = user,
            Channels = []
        };
    }

    public void CalculateWithRenderers(IEnumerable<IRenderer> renderers)
    {
        foreach (IRenderer renderer in renderers)
            CalculateWithRenderer(renderer);
    }

    private void CalculateWithRenderer(IRenderer renderer)
    {
        switch (renderer)
        {
            case ChannelRenderer channel:
                AddInfoForChannel(channel.Id);
                break;
            case VideoRenderer video:
                AddInfoForChannel(video.Channel.Id);
                break;
            case CompactVideoRenderer video:
                AddInfoForChannel(video.Channel.Id);
                break;
            case GridVideoRenderer video:
                AddInfoForChannel(video.Channel.Id);
                break;
            case PlaylistVideoRenderer video:
                AddInfoForChannel(video.Channel.Id);
                break;
            case PlaylistPanelVideoRenderer video:
                AddInfoForChannel(video.Channel.Id);
                break;

            case ShelfRenderer shelf:
                CalculateWithRenderers(shelf.Items);
                break;
            case ReelShelfRenderer shelf:
                CalculateWithRenderers(shelf.Items);
                break;
            case RichShelfRenderer shelf:
                CalculateWithRenderers(shelf.Contents);
                break;
            case SectionListRenderer list:
                CalculateWithRenderers(list.Contents);
                break;
            case ItemSectionRenderer section:
                CalculateWithRenderers(section.Contents);
                break;
            case RichSectionRenderer section:
                CalculateWithRenderer(section.Content);
                break;
            case GridRenderer grid:
                CalculateWithRenderers(grid.Items);
                break;
        }
    }

    public void AddInfoForChannel(string? channelId)
    {
        if (channelId == null) return;
        if (User.Subscriptions.ContainsKey(channelId) && !Channels.ContainsKey(channelId))
            Channels.Add(channelId, new ApiSubscriptionInfo(User.Subscriptions[channelId]));
    }
}