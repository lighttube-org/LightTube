using InnerTube;
using LightTube.Database.Models;

namespace LightTube.Contexts;

public class SubscriptionContext : ModalContext
{
    public InnerTubeChannelResponse Channel;
    public SubscriptionType CurrentType = SubscriptionType.NONE;

    public SubscriptionContext(HttpContext context, InnerTubeChannelResponse channel, SubscriptionType? subscriptionType = null) :
        base(context)
    {
        Channel = channel;
        if (!subscriptionType.HasValue)
            User?.Subscriptions.TryGetValue(channel.Header?.Id ?? "", out CurrentType);
        else
            CurrentType = subscriptionType.Value;
        Buttons =
        [
            new ModalButton("Go to channel", $"/channel/{channel.Header?.Id}", "secondary"),
            new ModalButton("", "|", ""),
            new ModalButton("Confirm", "__submit", "primary"),
        ];
        Title = "Manage Subscription";
    }
}