using InnerTube.Renderers;
using LightTube.Database.Models;

namespace LightTube.ApiModels;

public class ApiUserData
{
	public DatabaseUser User;
	public Dictionary<string, ApiSubscriptionInfo> Channels;

	public static ApiUserData? GetFromDatabaseUser(DatabaseUser? user)
	{
		if (user is null) return null;
		return new ApiUserData
		{
			User = user,
			Channels = new Dictionary<string, ApiSubscriptionInfo>()
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
				if (User.Subscriptions.ContainsKey(channel.Id))
					Channels.Add(channel.Id, new ApiSubscriptionInfo(User.Subscriptions[channel.Id]));
				break;
		}
	}
}