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

    public void CalculateWithRenderers(IEnumerable<RendererContainer> renderers)
    {
        foreach (RendererContainer renderer in renderers)
            CalculateWithRenderer(renderer);
    }

    private void CalculateWithRenderer(RendererContainer renderer)
    {
        switch (renderer.Type)
        {
            case "channel":
                AddInfoForChannel((renderer.Data as ChannelRendererData)?.ChannelId);
                break;
            case "video":
                AddInfoForChannel((renderer.Data as VideoRendererData)?.Author?.Id);
                break;
            case "container":
                CalculateWithRenderers((renderer.Data as ContainerRendererData)?.Items ?? []);
                break;
        }
    }

    public void AddInfoForChannel(string? channelId)
    {
        if (channelId == null) return;
        if (User.Subscriptions.TryGetValue(channelId, out SubscriptionType value) && !Channels.ContainsKey(channelId))
            Channels.Add(channelId, new ApiSubscriptionInfo(value));
    }
}