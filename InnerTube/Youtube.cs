using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using InnerTube.Models;
using Newtonsoft.Json.Linq;

namespace InnerTube
{
	public class Youtube
	{
		internal readonly HttpClient Client = new();

		public readonly Dictionary<string, CacheItem<YoutubePlayer>> PlayerCache = new();

		private async Task<JObject> MakeRequest(string endpoint, Dictionary<string, object> postData)
		{
			HttpRequestMessage hrm = new(HttpMethod.Post,
				@$"https://www.youtube.com/youtubei/v1/{endpoint}?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8");

			byte[] buffer = Encoding.UTF8.GetBytes(RequestContext.BuildRequestContextJson(postData));
			ByteArrayContent byteContent = new(buffer);
			byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			hrm.Content = byteContent;
			HttpResponseMessage ytPlayerRequest = await Client.SendAsync(hrm);

			return JObject.Parse(await ytPlayerRequest.Content.ReadAsStringAsync());
		}

		public async Task<YoutubePlayer> GetPlayerAsync(string videoId)
		{
			if (PlayerCache.Any(x => x.Key == videoId && x.Value.ExpireTime > DateTimeOffset.Now))
			{
				CacheItem<YoutubePlayer> item = PlayerCache[videoId];
				item.Item.ExpiresInSeconds = (item.ExpireTime - DateTimeOffset.Now).TotalSeconds.ToString(); 
				return item.Item;
			}

			YoutubePlayer player = await YtDlp.GetVideo(videoId).GetYoutubePlayer();
			PlayerCache.Add(videoId,
				new CacheItem<YoutubePlayer>(player,
					TimeSpan.FromSeconds(int.Parse(player.ExpiresInSeconds)).Subtract(TimeSpan.FromHours(1))));
			return player;
		}

		public async Task<YoutubeVideo> GetVideoAsync(string videoId)
		{
			JObject player = await MakeRequest("next", new Dictionary<string, object>
			{
				["videoId"] = videoId
			});

			YoutubeVideo video = new()
			{
				Id = player?["currentVideoEndpoint"]?["watchEndpoint"]?["videoId"]?.ToString(),
				Title = Utils.ReadRuns(
					player?["contents"]?["twoColumnWatchNextResults"]?["results"]?["results"]?["contents"]?[0]?
						["videoPrimaryInfoRenderer"]?["title"]?["runs"]?.ToObject<JArray>()),
				Description =
					Utils.ReadRuns(
						player?["contents"]?["twoColumnWatchNextResults"]?["results"]?["results"]?["contents"]?[1]?
							["videoSecondaryInfoRenderer"]?["description"]?["runs"]?.ToObject<JArray>()),
				Channel = new Channel
				{
					Name =
						player?["contents"]?["twoColumnWatchNextResults"]?["results"]?["results"]?["contents"]?[1]?
							["videoSecondaryInfoRenderer"]?["owner"]?["videoOwnerRenderer"]?["title"]?["runs"]?[0]?[
								"text"]?.ToString(),
					Id = player?["contents"]?["twoColumnWatchNextResults"]?["results"]?["results"]?["contents"]?[1]?
						["videoSecondaryInfoRenderer"]?["owner"]?["videoOwnerRenderer"]?["title"]?["runs"]?[0]?
						["navigationEndpoint"]?["browseEndpoint"]?["browseId"]?.ToString(),
					SubscriberCount =
						player?["contents"]?["twoColumnWatchNextResults"]?["results"]?["results"]?["contents"]?[1]?
							["videoSecondaryInfoRenderer"]?["owner"]?["videoOwnerRenderer"]?["subscriberCountText"]?[
								"simpleText"]?.ToString(),
					Avatars =
						(player?["contents"]?["twoColumnWatchNextResults"]?["results"]?["results"]?["contents"]?[1]?[
								"videoSecondaryInfoRenderer"]?["owner"]?["videoOwnerRenderer"]?["thumbnail"]?[
								"thumbnails"]
							?.ToObject<JArray>() ?? JArray.Parse("[]")).Select(Utils.ParseThumbnails).ToArray()
				},
				UploadDate =
					player?["contents"]?["twoColumnWatchNextResults"]?["results"]?["results"]?["contents"]?[0]?[
						"videoPrimaryInfoRenderer"]?["dateText"]?["simpleText"]?.ToString(),
				Recommended =
					ParseRecommendations(
						player?["contents"]?["twoColumnWatchNextResults"]?["secondaryResults"]?["secondaryResults"]?
							["results"]?.ToObject<JArray>() ?? JArray.Parse("[]"))
			};

			return video;
		}

		private DynamicItem[] ParseRecommendations(JArray recommendationsArray)
		{
			List<DynamicItem> items = new();

			foreach (JToken jToken in recommendationsArray)
			{
				JObject recommendationContainer = jToken as JObject;
				string rendererName = recommendationContainer?.First?.Path.Split(".").Last() ?? "";
				JObject recommendationItem = recommendationContainer?[rendererName]?.ToObject<JObject>();
				switch (rendererName)
				{
					case "compactVideoRenderer":
						items.Add(new VideoItem
						{
							Id = recommendationItem?["videoId"]?.ToString(),
							Title = recommendationItem?["title"]?["simpleText"]?.ToString(),
							Thumbnails =
								(recommendationItem?["thumbnail"]?["thumbnails"]?.ToObject<JArray>() ??
								 JArray.Parse("[]")).Select(Utils.ParseThumbnails).ToArray(),
							UploadedAt = recommendationItem?["publishedTimeText"]?["simpleText"]?.ToString(),
							Views = int.Parse(
								recommendationItem?["viewCountText"]?["simpleText"]?.ToString().Split(" ")[0]
									.Replace(",", "").Replace(".", "") ?? "0"),
							Channel = new Channel
							{
								Name = recommendationItem?["longBylineText"]?["runs"]?[0]?["text"]?.ToString(),
								Id = recommendationItem?["longBylineText"]?["runs"]?[0]?["navigationEndpoint"]?[
									"browseEndpoint"]?["browseId"]?.ToString(),
								SubscriberCount = null,
								Avatars = null
							},
							Duration = recommendationItem?["thumbnailOverlays"]?[0]?[
								"thumbnailOverlayTimeStatusRenderer"]?["text"]?["simpleText"]?.ToString()
						});
						break;
					case "compactPlaylistRenderer":
						items.Add(new PlaylistItem
						{
							Id = recommendationItem?["playlistId"]
								?.ToString(),
							Title = recommendationItem?["title"]?["simpleText"]
								?.ToString(),
							Thumbnails =
								(recommendationItem?["thumbnail"]?["thumbnails"]
									?.ToObject<JArray>() ?? JArray.Parse("[]")).Select(Utils.ParseThumbnails)
								.ToArray(),
							VideoCount = int.Parse(
								recommendationItem?["videoCountText"]?["runs"]?[0]?["text"]?.ToString().Replace(",", "")
									.Replace(".", "") ?? "0"),
							FirstVideoId = recommendationItem?["navigationEndpoint"]?["watchEndpoint"]?["videoId"]
								?.ToString(),
							Channel = new Channel
							{
								Name = recommendationItem?["longBylineText"]?["runs"]?[0]?["text"]
									?.ToString(),
								Id = recommendationItem?["longBylineText"]?["runs"]?[0]?["navigationEndpoint"]?[
										"browseEndpoint"]?["browseId"]
									?.ToString(),
								SubscriberCount = null,
								Avatars = null
							}
						});
						break;
					case "compactRadioRenderer":
						items.Add(new RadioItem
						{
							Id = recommendationItem?["playlistId"]
								?.ToString(),
							Title = recommendationItem?["title"]?["simpleText"]
								?.ToString(),
							Thumbnails =
								(recommendationItem?["thumbnail"]?["thumbnails"]
									?.ToObject<JArray>() ?? JArray.Parse("[]")).Select(Utils.ParseThumbnails)
								.ToArray(),
							FirstVideoId = recommendationItem?["navigationEndpoint"]?["watchEndpoint"]?["videoId"]
								?.ToString(),
							Channel = new Channel
							{
								Name = recommendationItem?["longBylineText"]?["simpleText"]?.ToString(),
								Id = "",
								SubscriberCount = null,
								Avatars = null
							}
						});
						break;
					case "continuationItemRenderer":
						items.Add(new ContinuationItem
						{
							Id = recommendationItem?["continuationEndpoint"]?["continuationCommand"]?["token"]?.ToString()
						});
						break;
					default:
						items.Add(new DynamicItem
						{
							Id = rendererName,
							Title = "Unknown Recommendation Type: " + rendererName
						});
						break;
				}
			}

			return items.ToArray();
		}
		/*
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
*/
	}
}