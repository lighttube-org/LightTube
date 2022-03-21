using InnerTube;
using InnerTube.Models;

namespace LightTube.Contexts
{
	public class PlayerContext : BaseContext
	{
		public YoutubePlayer Player;
		public YoutubeVideo Video;
		public YoutubeDislikes Engagement;
		public string Resolution;
		public bool CompatibilityMode;
	}
}