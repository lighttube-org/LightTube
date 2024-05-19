using InnerTube.Models;
using LightTube.ApiModels;
using LightTube.Database.Models;

namespace LightTube.Controllers;

public class UpdateSubscriptionResponse
{
    public string ChannelName { get; }
    public string ChannelAvatar { get; }
    public bool Subscribed { get; }
    public bool Notifications { get; }

    public UpdateSubscriptionResponse(InnerTubeChannel channel, SubscriptionType subscription)
    {
        try
        {
            ApiSubscriptionInfo info = new(subscription);
            Subscribed = info.Subscribed;
            Notifications = info.Notifications;
        }
        catch
        {
            Subscribed = false;
            Notifications = false;
        }

        ChannelName = channel.Metadata.Title;
        ChannelAvatar = channel.Metadata.Avatar.Thumbnails_.First().Url;
    }
}