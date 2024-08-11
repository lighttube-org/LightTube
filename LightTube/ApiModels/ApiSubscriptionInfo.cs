using LightTube.Database.Models;

namespace LightTube.ApiModels;

public class ApiSubscriptionInfo(SubscriptionType userSubscription)
{
    public bool Subscribed { get; } = userSubscription != SubscriptionType.NONE;
    public bool Notifications { get; } = userSubscription == SubscriptionType.NOTIFICATIONS_ON;
}