using LightTube.Database.Models;

namespace LightTube.Contexts;

public class SubscriptionsContext(HttpContext context) : BaseContext(context)
{
	public FeedVideo[] Videos;
}