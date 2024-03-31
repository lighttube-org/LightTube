using LightTube.Database.Models;

namespace LightTube.Contexts;

public class ChannelsContext : BaseContext
{
	public IEnumerable<DatabaseChannel?> Channels;

	public ChannelsContext(HttpContext context) : base(context)
	{ }
}