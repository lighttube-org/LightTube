using YTProxy.Models;

namespace LightTube.Contexts
{
	public class ChannelContext : BaseContext
	{
		public YoutubeChannel Channel;
		public string Id;
		public string ContinuationToken;
	}
}