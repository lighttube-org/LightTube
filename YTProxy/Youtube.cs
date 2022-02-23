using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YTProxy.Models;

namespace YTProxy
{
	public class Youtube
	{
		internal HttpClient Client;
		public YoutubeMusic Music;
		
		public Youtube(string apiRoot)
		{
			Client = new HttpClient
			{
				BaseAddress = new Uri(apiRoot)
			};
			Music = new YoutubeMusic(this);
		}

		public async Task<IEnumerable<string>> GetAllEndpoints()
		{
			string jsonDoc = await Client.GetStringAsync("/");
			return JObject.Parse(jsonDoc)["endpoints"]?.ToArray().Select(x => x.ToString()) ?? Array.Empty<string>();
		}

		public async Task<YoutubePlayer> GetPlayerAsync(string videoId)
		{
			string jsonDoc = await Client.GetStringAsync("/get_player_info?v=" + videoId);
			return JsonConvert.DeserializeObject<YoutubePlayer>(jsonDoc);
		}

		public async Task<YoutubeVideo> GetVideoAsync(string videoId)
		{
			string jsonDoc = await Client.GetStringAsync("/video?v=" + videoId);
			return JsonConvert.DeserializeObject<YoutubeVideo>(jsonDoc);
		}

		public async Task<YoutubeSearch> SearchAsync(string query, string continuation = null)
		{
			string jsonDoc = continuation == null
				? await Client.GetStringAsync("/search?q=" + query)
				: await Client.GetStringAsync("/search?continuation=" + continuation);
			return JsonConvert.DeserializeObject<YoutubeSearch>(jsonDoc);
		}

		public async Task<YoutubePlaylist> GetPlaylistAsync(string query, string continuation = null)
		{
			string jsonDoc = continuation == null
				? await Client.GetStringAsync("/playlist?id=" + query)
				: await Client.GetStringAsync("/playlist?continuation=" + continuation);
			return JsonConvert.DeserializeObject<YoutubePlaylist>(jsonDoc);
		}

		public async Task<YoutubeChannel> GetChannelAsync(string query, string continuation = null)
		{
			string jsonDoc = continuation == null
				? await Client.GetStringAsync("/channel?id=" + query)
				: await Client.GetStringAsync("/channel?continuation=" + continuation);
			return JsonConvert.DeserializeObject<YoutubeChannel>(jsonDoc);
		}
	}
}