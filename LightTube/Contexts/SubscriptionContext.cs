using InnerTube.Models;
using LightTube.Database.Models;

namespace LightTube.Contexts;

public class SubscriptionContext : ModalContext
{
    public InnerTubeChannel Channel;
    public SubscriptionType CurrentType = SubscriptionType.NONE;

    public SubscriptionContext(HttpContext context, InnerTubeChannel channel, SubscriptionType? subscriptionType = null) :
        base(context)
    {
        Channel = channel;
        if (!subscriptionType.HasValue)
            User?.Subscriptions.TryGetValue(channel.Header?.Id ?? "", out CurrentType);
        else
            CurrentType = subscriptionType.Value;
        Buttons =
        [
            new ModalButton(Localization.GetRawString("subscription.edit.channel"), $"/channel/{channel.Header?.Id}", "secondary"),
            new ModalButton("", "|", ""),
            new ModalButton(Localization.GetRawString("subscription.edit.confirm"), "__submit", "primary"),
        ];
        Title = Localization.GetRawString("subscription.edit.title");
    }
}