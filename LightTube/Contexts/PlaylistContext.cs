using InnerTube.Models;

namespace LightTube.Contexts
{
	public class PlaylistContext : BaseContext
	{
		public YoutubePlaylist Playlist;
		public string Id;
		public string Message;
		public bool Editable;
		public string ContinuationToken;
	}
}