namespace LightTube.Controllers;

public class UpdateSubscriptionRequest
{
    public string ChannelId;
    public bool Subscribed;
    public bool EnableNotifications;
}