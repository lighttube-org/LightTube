using LightTube.Database.Models;

namespace LightTube.Contexts
{
	public class HomepageContext : BaseContext
	{
		public FeedVideo[] Videos;
		public HomepageContext(HttpContext context) : base(context)
		{
			AddRSSUrl(context.Request.Scheme + "://" + context.Request.Host + "/feed/rss.xml");
			if (User != null)
			{
				Videos = Task.Run(async () =>
				{
					return (await YoutubeRSS.GetMultipleFeeds(User.Subscriptions.Where(x => x.Value == SubscriptionType.NOTIFICATIONS_ON).Select(x => x.Key))).Take(context.Request.Cookies["maxvideos"] is null ? 5:Convert.ToInt32(context.Request.Cookies["maxvideos"])).ToArray();
				}).Result;
			}
		}
	}
}
