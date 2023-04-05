using InnerTube;
using LightTube.ApiModels;
using LightTube.Database.Models;

namespace LightTube.Controllers;

public class UpdateSubscriptionResponse
{
	public string ChannelName { get; }
	public string ChannelAvatar { get; }
	public bool Subscribed { get; }
	public bool Notifications { get; }

	public UpdateSubscriptionResponse(InnerTubeChannelResponse channel, DatabaseUser user)
	{
		try
		{
			ApiSubscriptionInfo info = new(user.Subscriptions[channel.Metadata.Id]);
			Subscribed = info.Subscribed;
			Notifications = info.Notifications;
		}
		catch
		{
			Subscribed = false;
			Notifications = false;
		}

		ChannelName = channel.Metadata.Title;
		ChannelAvatar = channel.Metadata.Avatar.Last().Url.ToString();
	}
}