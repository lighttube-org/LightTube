using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using YTProxy.Models;

namespace YTProxy
{
	public class YoutubeMusic
	{
		private HttpClient Client;
		private Youtube Youtube;

		internal YoutubeMusic(Youtube youtube)
		{
			Youtube = youtube;
			Client = youtube.Client;
		}

		public async Task<YoutubeMusicSearch> SearchAsync(string query)
		{
			string jsonDoc = await Client.GetStringAsync($"/ytmusic/search?q={query}");
			return JsonConvert.DeserializeObject<YoutubeMusicSearch>(jsonDoc);
		}

		public async Task<YoutubeMusicMoreSearch> SearchAsync(string query, string @params)
		{
			string jsonDoc = await Client.GetStringAsync($"/ytmusic/search?q={query}&params={@params}");
			return JsonConvert.DeserializeObject<YoutubeMusicMoreSearch>(jsonDoc);
		}

		public async Task<YoutubeMusicMoreSearch> SearchMoreAsync(string continuation)
		{
			string jsonDoc = await Client.GetStringAsync($"/ytmusic/search?continuation={continuation}");
			return JsonConvert.DeserializeObject<YoutubeMusicMoreSearch>(jsonDoc);
		}

		public async Task<YoutubeArtist> ArtistAsync(string id)
		{
			string jsonDoc = await Client.GetStringAsync($"/ytmusic/artist?id={id}");
			return JsonConvert.DeserializeObject<YoutubeArtist>(jsonDoc);
		}

		public async Task<YoutubeAlbum> AlbumAsync(string id)
		{
			string jsonDoc = await Client.GetStringAsync($"/ytmusic/album?id={id}");
			return JsonConvert.DeserializeObject<YoutubeAlbum>(jsonDoc);
		}
	}
}