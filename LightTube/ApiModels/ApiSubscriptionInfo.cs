using LightTube.Database.Models;

namespace LightTube.ApiModels;

public class ApiSubscriptionInfo
{
	public bool Subscribed { get; }
	public bool Notifications { get; }
	
	public ApiSubscriptionInfo(SubscriptionType userSubscription)
	{
		Subscribed = userSubscription != SubscriptionType.NONE;
		Notifications = userSubscription == SubscriptionType.NOTIFICATIONS_ON;
	}
}