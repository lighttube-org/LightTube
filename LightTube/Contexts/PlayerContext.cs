using YTProxy.Models;

namespace LightTube.Contexts
{
	public class PlayerContext : BaseContext
	{
		public YoutubePlayer Player;
		public YoutubeVideo Video;
		public string Resolution;
	}
}