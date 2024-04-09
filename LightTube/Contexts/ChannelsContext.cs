using LightTube.Database.Models;

namespace LightTube.Contexts;

public class ChannelsContext(HttpContext context) : BaseContext(context)
{
    public IEnumerable<DatabaseChannel?> Channels;
}