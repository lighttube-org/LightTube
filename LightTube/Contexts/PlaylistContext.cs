using YTProxy.Models;

namespace LightTube.Contexts
{
	public class PlaylistContext : BaseContext
	{
		public YoutubePlaylist Playlist;
		public string Id;
		public string ContinuationToken;
	}
}