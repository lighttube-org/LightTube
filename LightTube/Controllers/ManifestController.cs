using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using InnerTube;
using InnerTube.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace LightTube.Controllers
{
	[Route("/manifest")]
	public class ManifestController : Controller
	{
		private readonly Youtube _youtube;
		private readonly HttpClient _client = new();

		public ManifestController(Youtube youtube)
		{
			_youtube = youtube;
		}

		[Route("{v}")]
		public async Task<IActionResult> DefaultManifest(string v)
		{
			YoutubePlayer player = await _youtube.GetPlayerAsync(v, HttpContext.GetLanguage(), HttpContext.GetRegion());
			if (!string.IsNullOrWhiteSpace(player.ErrorMessage))
				return StatusCode(500, player.ErrorMessage);
			return Redirect(player.IsLive ? $"/manifest/{v}.m3u8" : $"/manifest/{v}.mpd" + Request.QueryString);
		}

		[Route("{v}.mpd")]
		public async Task<IActionResult> DashManifest(string v, string videoCodec = null, string audioCodec = null, bool useProxy = true)
		{
			YoutubePlayer player = await _youtube.GetPlayerAsync(v, HttpContext.GetLanguage(), HttpContext.GetRegion());
			string manifest = player.GetMpdManifest(useProxy ? $"https://{Request.Host}/proxy/" : null, videoCodec, audioCodec);
			return File(Encoding.UTF8.GetBytes(manifest), "application/dash+xml");
		}

		[Route("{v}.m3u8")]
		public async Task<IActionResult> HlsManifest(string v, bool useProxy = true)
		{
			YoutubePlayer player = await _youtube.GetPlayerAsync(v, HttpContext.GetLanguage(), HttpContext.GetRegion());
			string manifest = player.GetHlsManifest(useProxy ? $"https://{Request.Host}/proxy" : null);
			return File(Encoding.UTF8.GetBytes(manifest), "application/vnd.apple.mpegurl");
		}

		[Route("hls/{v}.m3u8")]
		public async Task<IActionResult> IosHlsManifest(string v, bool useProxy = false) 
		{
			//todo: proxy
			if (useProxy) 
				return NotFound("Proxies for HLS manifests are not available at the moment.");
			
			//todo: cache

			string sapisid = Environment.GetEnvironmentVariable("SAPISID");
			string psid = Environment.GetEnvironmentVariable("PSID");
			
			HttpRequestMessage hrm = new(HttpMethod.Post,
				"https://www.youtube.com/youtubei/v1/player?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8");
			
			byte[] buffer = Encoding.UTF8.GetBytes(
				RequestContext.BuildRequestContextJson(new Dictionary<string, object>
				{
					["videoId"] = v
				}, clientName: "IOS", clientVersion: "17.13.3"));
			ByteArrayContent byteContent = new(buffer);
			byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			hrm.Content = byteContent;
			
			long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			string hashInput = $"{timestamp} {sapisid} https://www.youtube.com";
			using SHA1Managed sha1 = new();
			byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
			StringBuilder sb = new(hash.Length * 2);
			foreach (byte b in hash) sb.Append(b.ToString("X2"));
			string hashDigest = sb.ToString();
			
			if (sapisid is not null && psid is not null)
			{
				hrm.Headers.Add("Cookie", $"SAPISID={sapisid}; __Secure-3PAPISID={sapisid}; __Secure-3PSID={psid};");
				hrm.Headers.Add("Authorization", $"SAPISIDHASH {timestamp}_{hashDigest}");
				hrm.Headers.Add("X-Origin", "https://www.youtube.com");
				hrm.Headers.Add("X-Youtube-Client-Name", "5");
				hrm.Headers.Add("X-Youtube-Client-Version", "17.13.3");
				hrm.Headers.Add("Accept-Language", "en-US;q=0.8,en;q=0.7");
				hrm.Headers.Add("Origin", "https://www.youtube.com");
				hrm.Headers.Add("Referer", "https://www.youtube.com/watch?v=" + v);
			}

			HttpResponseMessage ytPlayerRequest = await _client.SendAsync(hrm);
			JObject response = JObject.Parse(await ytPlayerRequest.Content.ReadAsStringAsync());

			if (response["playabilityStatus"]?["status"]?.ToString() == "OK")
				return Redirect(response["streamingData"]?["hlsManifestUrl"]?.ToString());
			return NotFound($"{response["playabilityStatus"]?["status"] ?? response}\n{response["playabilityStatus"]?["errorScreen"]?["confirmDialogRenderer"]?["title"]?["runs"]?[0]?["text"]}\n{response["playabilityStatus"]?["errorScreen"]?["confirmDialogRenderer"]?["dialogMessages"]?[0]?["runs"]?[0]?["text"]}");
		}
	}
}