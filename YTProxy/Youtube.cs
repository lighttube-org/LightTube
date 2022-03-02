using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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

		public Dictionary<string, CacheItem<YoutubePlayer>> PlayerCache = new();

		public async Task<IEnumerable<string>> GetAllEndpoints()
		{
			string jsonDoc = await Client.GetStringAsync("/");
			return JObject.Parse(jsonDoc)["endpoints"]?.ToArray().Select(x => x.ToString()) ?? Array.Empty<string>();
		}

		public async Task<YoutubePlayer> GetPlayerAsync(string videoId, string language, string region)
		{
			if (PlayerCache.Any(x => x.Key == videoId && x.Value.ExpireTime > DateTimeOffset.Now))
				return PlayerCache[videoId].Item;
			YoutubePlayer player = await MakeRequest<YoutubePlayer>("/get_player_info?v=" + videoId, language, region);

			// Do not cache error messages and live videos
			if (string.IsNullOrWhiteSpace(player.ErrorMessage) && !(player.AdaptiveFormats.Length > 0 && player.Formats.Length == 0))
			{
				if (PlayerCache.ContainsKey(videoId))
					PlayerCache.Remove(videoId);
				PlayerCache.Add(videoId,
					new CacheItem<YoutubePlayer>(player,
						TimeSpan.FromSeconds(int.Parse(player.ExpiresInSeconds)).Subtract(TimeSpan.FromHours(1))));
			}

			return player;
		}

		public async Task<YoutubeVideo> GetVideoAsync(string videoId, string language, string region)
		{
			return await MakeRequest<YoutubeVideo>("/video?v=" + videoId, language, region);
		}

		public async Task<YoutubeSearch> SearchAsync(string query, string language, string region, string continuation = null)
		{
			return continuation == null
				? await MakeRequest<YoutubeSearch>("/search?q=" + query, language, region)
				: await MakeRequest<YoutubeSearch>("/search?continuation=" + continuation, language, region);
		}

		public async Task<YoutubePlaylist> GetPlaylistAsync(string id, string language, string region, string continuation = null)
		{
			return continuation == null
				? await MakeRequest<YoutubePlaylist>("/playlist?id=" + id, language, region)
				: await MakeRequest<YoutubePlaylist>("/playlist?continuation=" + continuation, language, region);
		}

		public async Task<YoutubeChannel> GetChannelAsync(string id, string language, string region, string continuation = null)
		{
			return continuation == null
				? await MakeRequest<YoutubeChannel>("/channel?id=" + id, language, region)
				: await MakeRequest<YoutubeChannel>("/channel?continuation=" + continuation, language, region);
		}

		public async Task<YoutubeLocals> GetLocalsAsync()
		{
			return await MakeRequest<YoutubeLocals>("/locals", "en", "US");
		}

		private async Task<T> MakeRequest<T>(string url, string hl, string gl)
		{
			HttpRequestMessage request = new(HttpMethod.Get, url);
			request.Headers.Add("X-Content-Language", hl);
			request.Headers.Add("X-Content-Region", gl);
			HttpResponseMessage response = await Client.SendAsync(request);
			string jsonDoc = await response.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<T>(jsonDoc);
		}
	}
}