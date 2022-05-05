using System.Collections.Generic;
using InnerTube.Models;
using LightTube.Database;

namespace LightTube.Contexts
{
	public class AddToPlaylistContext : BaseContext
	{
		public string Id;
		public YoutubeVideo Video;
		public string Thumbnail;
		public IEnumerable<LTPlaylist> Playlists;
	}
}