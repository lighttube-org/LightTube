using LightTube.Database.Models;

namespace LightTube.Contexts;

public class SubscriptionsContext : BaseContext
{
	public FeedVideo[] Videos;

	public SubscriptionsContext(HttpContext context) : base(context)
	{ }
}