using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		private readonly Dictionary<ChannelTabs, string> ChannelTabParams = new()
		{
			[ChannelTabs.Home] = @"EghmZWF0dXJlZA%3D%3D",
			[ChannelTabs.Videos] = @"EgZ2aWRlb3M%3D",
			[ChannelTabs.Playlists] = @"EglwbGF5bGlzdHM%3D",
			[ChannelTabs.Community] = @"Egljb21tdW5pdHk%3D",
			[ChannelTabs.Channels] = @"EghjaGFubmVscw%3D%3D",
			[ChannelTabs.About] = @"EgVhYm91dA%3D%3D"
		};

		private async Task<JObject> MakeRequest(string endpoint, Dictionary<string, object> postData, string language,
			string region)
		{
			HttpRequestMessage hrm = new(HttpMethod.Post,
				@$"https://www.youtube.com/youtubei/v1/{endpoint}?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8");

			byte[] buffer = Encoding.UTF8.GetBytes(RequestContext.BuildRequestContextJson(postData, language, region));
			ByteArrayContent byteContent = new(buffer);
			byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			hrm.Content = byteContent;
			HttpResponseMessage ytPlayerRequest = await Client.SendAsync(hrm);

			return JObject.Parse(await ytPlayerRequest.Content.ReadAsStringAsync());
		}

		public async Task<YoutubePlayer> GetPlayerAsync(string videoId, string language = "en", string region = "US")
		{
			if (PlayerCache.Any(x => x.Key == videoId && x.Value.ExpireTime > DateTimeOffset.Now))
			{
				CacheItem<YoutubePlayer> item = PlayerCache[videoId];
				item.Item.ExpiresInSeconds = ((int)(item.ExpireTime - DateTimeOffset.Now).TotalSeconds).ToString();
				return item.Item;
			}

			try {
				YoutubePlayer player = await YtDlp.GetVideo(videoId).GetYoutubePlayer();
				PlayerCache.Remove(videoId);
				PlayerCache.Add(videoId,
					new CacheItem<YoutubePlayer>(player,
						TimeSpan.FromSeconds(int.Parse(player.ExpiresInSeconds ?? "21600")).Subtract(TimeSpan.FromHours(1))));
				return player;
			} 
			catch (YtDlpException e)
			{
				return new YoutubePlayer
				{
					Id = "",
					Title = "",
					Description = "",
					Categories = Array.Empty<string>(),
					Tags = Array.Empty<string>(),
					Channel = new Channel
					{
						Name = "",
						Id = "",
						SubscriberCount = "",
						Avatars = Array.Empty<Thumbnail>()
					},
					UploadDate = "",
					Duration = 0,
					Chapters = Array.Empty<Chapter>(),
					Thumbnails = Array.Empty<Thumbnail>(),
					Formats = Array.Empty<Format>(),
					AdaptiveFormats = Array.Empty<Format>(),
					Subtitles = Array.Empty<Subtitle>(),
					Storyboards = Array.Empty<Format>(),
					ExpiresInSeconds = "0",
					ErrorMessage = e.ErrorMessage
				};
			}
		}

		public async Task<YoutubeVideo> GetVideoAsync(string videoId, string language = "en", string region = "US")
		{
			JObject player = await MakeRequest("next", new Dictionary<string, object>
			{
				["videoId"] = videoId
			}, language, region);

			JToken[] contents =
				(player?["contents"]?["twoColumnWatchNextResults"]?["results"]?["results"]?["contents"]
						?.ToObject<JArray>() ?? new JArray())
					.SkipWhile(x => !x.First.Path.EndsWith("videoPrimaryInfoRenderer")).ToArray();

			YoutubeVideo video = new()
			{
				Id = player["currentVideoEndpoint"]?["watchEndpoint"]?["videoId"]?.ToString(),
				Title = Utils.ReadRuns(
					contents[0]
						["videoPrimaryInfoRenderer"]?["title"]?["runs"]?.ToObject<JArray>()),
				Description =
					Utils.ReadRuns(
						contents[1]
							["videoSecondaryInfoRenderer"]?["description"]?["runs"]?.ToObject<JArray>()),
				Channel = new Channel
				{
					Name =
						contents[1]
							["videoSecondaryInfoRenderer"]?["owner"]?["videoOwnerRenderer"]?["title"]?["runs"]?[0]?[
								"text"]?.ToString(),
					Id = contents[1]
						["videoSecondaryInfoRenderer"]?["owner"]?["videoOwnerRenderer"]?["title"]?["runs"]?[0]?
						["navigationEndpoint"]?["browseEndpoint"]?["browseId"]?.ToString(),
					SubscriberCount =
						contents[1]
							["videoSecondaryInfoRenderer"]?["owner"]?["videoOwnerRenderer"]?["subscriberCountText"]?[
								"simpleText"]?.ToString(),
					Avatars =
						(contents[1][
								"videoSecondaryInfoRenderer"]?["owner"]?["videoOwnerRenderer"]?["thumbnail"]?[
								"thumbnails"]
							?.ToObject<JArray>() ?? new JArray()).Select(Utils.ParseThumbnails).ToArray()
				},
				UploadDate =
					contents[0][
						"videoPrimaryInfoRenderer"]?["dateText"]?["simpleText"]?.ToString(),
				Recommended =
					ParseRenderers(
						player?["contents"]?["twoColumnWatchNextResults"]?["secondaryResults"]?["secondaryResults"]?
							["results"]?.ToObject<JArray>() ?? new JArray())
			};

			return video;
		}

		public async Task<YoutubeSearchResults> SearchAsync(string query, string continuation = null,
			string language = "en", string region = "US")
		{
			Dictionary<string, object> data = new();
			if (string.IsNullOrWhiteSpace(continuation))
				data.Add("query", query);
			else
				data.Add("continuation", continuation);
			JObject search = await MakeRequest("search", data, language, region);

			return new YoutubeSearchResults
			{
				Refinements = search?["refinements"]?.ToObject<string[]>() ?? Array.Empty<string>(),
				EstimatedResults = search?["estimatedResults"]?.ToObject<long>() ?? 0,
				Results = ParseRenderers(
					search?["contents"]?["twoColumnSearchResultsRenderer"]?["primaryContents"]?["sectionListRenderer"]?
						["contents"]?[0]?["itemSectionRenderer"]?["contents"]?.ToObject<JArray>() ??
					search?["onResponseReceivedCommands"]?[0]?["appendContinuationItemsAction"]?["continuationItems"]?
						[0]?["itemSectionRenderer"]?["contents"]?.ToObject<JArray>() ?? new JArray()),
				ContinuationKey =
					search?["contents"]?["twoColumnSearchResultsRenderer"]?["primaryContents"]?["sectionListRenderer"]?
						["contents"]?[1]?["continuationItemRenderer"]?["continuationEndpoint"]?["continuationCommand"]?
						["token"]?.ToString() ??
					search?["onResponseReceivedCommands"]?[0]?["appendContinuationItemsAction"]?["continuationItems"]?
						[1]?["continuationItemRenderer"]?["continuationEndpoint"]?["continuationCommand"]?["token"]
						?.ToString() ?? ""
			};
		}

		public async Task<YoutubePlaylist> GetPlaylistAsync(string id, string continuation = null,
			string language = "en", string region = "US")
		{
			Dictionary<string, object> data = new();
			if (string.IsNullOrWhiteSpace(continuation))
				data.Add("browseId", "VL" + id);
			else
				data.Add("continuation", continuation);
			JObject playlist = await MakeRequest("browse", data, language, region);
			DynamicItem[] renderers = ParseRenderers(
				playlist?["contents"]?["twoColumnBrowseResultsRenderer"]?["tabs"]?[0]?["tabRenderer"]?["content"]?
					["sectionListRenderer"]?["contents"]?[0]?["itemSectionRenderer"]?["contents"]?[0]?
					["playlistVideoListRenderer"]?["contents"]?.ToObject<JArray>() ??
				playlist?["onResponseReceivedActions"]?[0]?["appendContinuationItemsAction"]?["continuationItems"]
					?.ToObject<JArray>() ?? new JArray());

			return new YoutubePlaylist
			{
				Id = id,
				Title = playlist?["metadata"]?["playlistMetadataRenderer"]?["title"]?.ToString(),
				Description = playlist?["metadata"]?["playlistMetadataRenderer"]?["description"]?.ToString(),
				VideoCount = playlist?["sidebar"]?["playlistSidebarRenderer"]?["items"]?[0]?[
					"playlistSidebarPrimaryInfoRenderer"]?["stats"]?[0]?["runs"]?[0]?["text"]?.ToString(),
				ViewCount = playlist?["sidebar"]?["playlistSidebarRenderer"]?["items"]?[0]?[
					"playlistSidebarPrimaryInfoRenderer"]?["stats"]?[1]?["simpleText"]?.ToString(),
				LastUpdated = Utils.ReadRuns(playlist?["sidebar"]?["playlistSidebarRenderer"]?["items"]?[0]?[
					"playlistSidebarPrimaryInfoRenderer"]?["stats"]?[2]?["runs"]?.ToObject<JArray>() ?? new JArray()),
				Thumbnail = (playlist?["microformat"]?["microformatDataRenderer"]?["thumbnail"]?["thumbnails"] ??
				             new JArray()).Select(Utils.ParseThumbnails).ToArray(),
				Channel = new Channel
				{
					Name =
						playlist?["sidebar"]?["playlistSidebarRenderer"]?["items"]?[1]?
							["playlistSidebarSecondaryInfoRenderer"]?["videoOwner"]?["videoOwnerRenderer"]?["title"]?
							["runs"]?[0]?["text"]?.ToString(),
					Id = playlist?["sidebar"]?["playlistSidebarRenderer"]?["items"]?[1]?
						["playlistSidebarSecondaryInfoRenderer"]?["videoOwner"]?["videoOwnerRenderer"]?
						["navigationEndpoint"]?["browseEndpoint"]?["browseId"]?.ToString(),
					SubscriberCount = "",
					Avatars =
						(playlist?["sidebar"]?["playlistSidebarRenderer"]?["items"]?[1]?
							["playlistSidebarSecondaryInfoRenderer"]?["videoOwner"]?["videoOwnerRenderer"]?["thumbnail"]
							?["thumbnails"] ?? new JArray()).Select(Utils.ParseThumbnails).ToArray()
				},
				Videos = renderers.Where(x => x is not ContinuationItem).ToArray(),
				ContinuationKey = renderers.FirstOrDefault(x => x is ContinuationItem)?.Id
			};
		}

		public async Task<YoutubeChannel> GetChannelAsync(string id, ChannelTabs tab = ChannelTabs.Home,
			string continuation = null, string language = "en", string region = "US")
		{
			Dictionary<string, object> data = new();
			if (string.IsNullOrWhiteSpace(continuation))
			{
				data.Add("browseId", id);
				if (string.IsNullOrWhiteSpace(continuation))
					data.Add("params", ChannelTabParams[tab]);
			}
			else
			{
				data.Add("continuation", continuation);
			}

			JObject channel = await MakeRequest("browse", data, language, region);
			JArray mainArray =
				(channel?["contents"]?["twoColumnBrowseResultsRenderer"]?["tabs"]?.ToObject<JArray>() ?? new JArray())
				.FirstOrDefault(x => x?["tabRenderer"]?["selected"]?.ToObject<bool>() ?? false)?["tabRenderer"]?[
					"content"]?
				["sectionListRenderer"]?["contents"]?.ToObject<JArray>();

			return new YoutubeChannel
			{
				Id = channel?["metadata"]?["channelMetadataRenderer"]?["externalId"]?.ToString(),
				Name = channel?["metadata"]?["channelMetadataRenderer"]?["title"]?.ToString(),
				Url = channel?["metadata"]?["channelMetadataRenderer"]?["externalId"]?.ToString(),
				Avatars = (channel?["metadata"]?["channelMetadataRenderer"]?["avatar"]?["thumbnails"] ?? new JArray())
					.Select(Utils.ParseThumbnails).ToArray(),
				Banners = (channel?["header"]?["c4TabbedHeaderRenderer"]?["banner"]?["thumbnails"] ?? new JArray())
					.Select(Utils.ParseThumbnails).ToArray(),
				Description = channel?["metadata"]?["channelMetadataRenderer"]?["description"]?.ToString(),
				Videos = ParseRenderers(mainArray ??
				                        channel?["onResponseReceivedActions"]?[0]?["appendContinuationItemsAction"]?
					                        ["continuationItems"]?.ToObject<JArray>() ?? new JArray()),
				Subscribers = channel?["header"]?["c4TabbedHeaderRenderer"]?["subscriberCountText"]?["simpleText"]
					?.ToString()
			};
		}

		public async Task<YoutubeTrends> GetExploreAsync(string browseId = null, string continuation = null, string language = "en", string region = "US")
		{
			Dictionary<string, object> data = new();
			if (string.IsNullOrWhiteSpace(continuation))
			{
				data.Add("browseId", browseId ?? "FEexplore");
			}
			else
			{
				data.Add("continuation", continuation);
			}

			JObject explore = await MakeRequest("browse", data, language, region);
			JToken[] token =
				(explore?["contents"]?["twoColumnBrowseResultsRenderer"]?["tabs"]?[0]?["tabRenderer"]?["content"]?
					["sectionListRenderer"]?["contents"]?.ToObject<JArray>() ?? new JArray()).Skip(1).ToArray();

			JArray mainArray = new(token.Select(x => x is JObject obj ? obj : null).Where(x => x is not null));

			return new YoutubeTrends
			{
				Categories = explore?["contents"]?["twoColumnBrowseResultsRenderer"]?["tabs"]?[0]?["tabRenderer"]?["content"]?["sectionListRenderer"]?["contents"]?[0]?["itemSectionRenderer"]?["contents"]?[0]?["destinationShelfRenderer"]?["destinationButtons"]?.Select(
					x =>
					{
						JToken rendererObject = x?["destinationButtonRenderer"];
						TrendCategory category = new()
						{
							Label = rendererObject?["label"]?["simpleText"]?.ToString(),
							BackgroundImage = (rendererObject?["backgroundImage"]?["thumbnails"]?.ToObject<JArray>() ??
							                   new JArray()).Select(Utils.ParseThumbnails).ToArray(),
							Icon = (rendererObject?["iconImage"]?["thumbnails"]?.ToObject<JArray>() ??
							        new JArray()).Select(Utils.ParseThumbnails).ToArray(),
							Id = $"{rendererObject?["onTap"]?["browseEndpoint"]?["browseId"]}"
						};
						return category;
					}).ToArray(),
				Videos = ParseRenderers(mainArray)
			};
		}

		public async Task<YoutubeLocals> GetLocalsAsync(string language = "en", string region = "US")
		{
			JObject locals = await MakeRequest("account/account_menu", new Dictionary<string, object>(), language,
				region);

			return new YoutubeLocals
			{
				Languages =
					locals["actions"]?[0]?["openPopupAction"]?["popup"]?["multiPageMenuRenderer"]?["sections"]?[0]?
						["multiPageMenuSectionRenderer"]?["items"]?[1]?["compactLinkRenderer"]?["serviceEndpoint"]?
						["signalServiceEndpoint"]?["actions"]?[0]?["getMultiPageMenuAction"]?["menu"]?
						["multiPageMenuRenderer"]?["sections"]?[0]?["multiPageMenuSectionRenderer"]?["items"]?
						.ToObject<JArray>()?.ToDictionary(
							x => x?["compactLinkRenderer"]?["serviceEndpoint"]?["signalServiceEndpoint"]?
								["actions"]?[0]?["selectLanguageCommand"]?["hl"]?.ToString(),
							x => x?["compactLinkRenderer"]?["title"]?["simpleText"]?.ToString()),
				Regions =
					locals["actions"]?[0]?["openPopupAction"]?["popup"]?["multiPageMenuRenderer"]?["sections"]?[0]?
						["multiPageMenuSectionRenderer"]?["items"]?[2]?["compactLinkRenderer"]?["serviceEndpoint"]?
						["signalServiceEndpoint"]?["actions"]?[0]?["getMultiPageMenuAction"]?["menu"]?
						["multiPageMenuRenderer"]?["sections"]?[0]?["multiPageMenuSectionRenderer"]?["items"]?
						.ToObject<JArray>()?.ToDictionary(
							x => x?["compactLinkRenderer"]?["serviceEndpoint"]?["signalServiceEndpoint"]?
								["actions"]?[0]?["selectCountryCommand"]?["gl"]?.ToString(),
							x => x?["compactLinkRenderer"]?["title"]?["simpleText"]?.ToString())
			};
		}

		private DynamicItem[] ParseRenderers(JArray renderersArray)
		{
			List<DynamicItem> items = new();

			foreach (JToken jToken in renderersArray)
			{
				JObject recommendationContainer = jToken as JObject;
				string rendererName = recommendationContainer?.First?.Path.Split(".").Last() ?? "";
				JObject rendererItem = recommendationContainer?[rendererName]?.ToObject<JObject>();
				switch (rendererName)
				{
					case "videoRenderer":
						items.Add(new VideoItem
						{
							Id = rendererItem?["videoId"]?.ToString(),
							Title = Utils.ReadRuns(rendererItem?["title"]?["runs"]?.ToObject<JArray>() ??
							                       new JArray()),
							Thumbnails =
								(rendererItem?["thumbnail"]?["thumbnails"]?.ToObject<JArray>() ??
								 new JArray()).Select(Utils.ParseThumbnails).ToArray(),
							UploadedAt = rendererItem?["publishedTimeText"]?["simpleText"]?.ToString(),
							Views = long.TryParse(
								rendererItem?["viewCountText"]?["simpleText"]?.ToString().Split(" ")[0]
									.Replace(",", "").Replace(".", "") ?? "0", out long vV) ? vV : 0,
							Channel = new Channel
							{
								Name = rendererItem?["longBylineText"]?["runs"]?[0]?["text"]?.ToString(),
								Id = rendererItem?["longBylineText"]?["runs"]?[0]?["navigationEndpoint"]?[
									"browseEndpoint"]?["browseId"]?.ToString(),
								SubscriberCount = null,
								Avatars =
									(rendererItem?["channelThumbnailSupportedRenderers"]?[
											"channelThumbnailWithLinkRenderer"]?["thumbnail"]?["thumbnails"]
										?.ToObject<JArray>() ?? new JArray()).Select(Utils.ParseThumbnails)
									.ToArray()
							},
							Duration = rendererItem?["thumbnailOverlays"]?[0]?[
								"thumbnailOverlayTimeStatusRenderer"]?["text"]?["simpleText"]?.ToString(),
							Description = Utils.ReadRuns(rendererItem?["detailedMetadataSnippets"]?[0]?[
								"snippetText"]?["runs"]?.ToObject<JArray>() ?? new JArray())
						});
						break;
					case "gridVideoRenderer":
						items.Add(new VideoItem
						{
							Id = rendererItem?["videoId"]?.ToString(),
							Title = rendererItem?["title"]?["simpleText"]?.ToString() ?? Utils.ReadRuns(
								rendererItem?["title"]?["runs"]?.ToObject<JArray>() ?? new JArray()),
							Thumbnails =
								(rendererItem?["thumbnail"]?["thumbnails"]?.ToObject<JArray>() ??
								 new JArray()).Select(Utils.ParseThumbnails).ToArray(),
							UploadedAt = rendererItem?["publishedTimeText"]?["simpleText"]?.ToString(),
							Views = long.TryParse(
								rendererItem?["viewCountText"]?["simpleText"]?.ToString().Split(" ")[0]
									.Replace(",", "").Replace(".", "") ?? "0", out long gVV) ? gVV : 0,
							Channel = null,
							Duration = rendererItem?["thumbnailOverlays"]?[0]?[
								"thumbnailOverlayTimeStatusRenderer"]?["text"]?["simpleText"]?.ToString()
						});
						break;
					case "playlistRenderer":
						items.Add(new PlaylistItem
						{
							Id = rendererItem?["playlistId"]
								?.ToString(),
							Title = rendererItem?["title"]?["simpleText"]
								?.ToString(),
							Thumbnails =
								(rendererItem?["thumbnails"]?[0]?["thumbnails"]?.ToObject<JArray>() ??
								 new JArray()).Select(Utils.ParseThumbnails).ToArray(),
							VideoCount = int.TryParse(
								rendererItem?["videoCountText"]?["runs"]?[0]?["text"]?.ToString().Replace(",", "")
									.Replace(".", "") ?? "0", out int pVC) ? pVC : 0,
							FirstVideoId = rendererItem?["navigationEndpoint"]?["watchEndpoint"]?["videoId"]
								?.ToString(),
							Channel = new Channel
							{
								Name = rendererItem?["longBylineText"]?["runs"]?[0]?["text"]
									?.ToString(),
								Id = rendererItem?["longBylineText"]?["runs"]?[0]?["navigationEndpoint"]?[
										"browseEndpoint"]?["browseId"]
									?.ToString(),
								SubscriberCount = null,
								Avatars = null
							}
						});
						break;
					case "channelRenderer":
						items.Add(new ChannelItem
						{
							Id = rendererItem?["channelId"]?.ToString(),
							Title = rendererItem?["title"]?["simpleText"]?.ToString(),
							Thumbnails =
								(rendererItem?["thumbnail"]?["thumbnails"]
									 ?.ToObject<JArray>() ??
								 new JArray()).Select(Utils.ParseThumbnails)
								.ToArray(), //
							Url = rendererItem?["navigationEndpoint"]?["commandMetadata"]?["webCommandMetadata"]?["url"]
								?.ToString(),
							Description =
								Utils.ReadRuns(rendererItem?["descriptionSnippet"]?["runs"]?.ToObject<JArray>() ??
								               new JArray()),
							VideoCount = long.TryParse(
								rendererItem?["videoCountText"]?["runs"]?[0]?["text"]
									?.ToString()
									.Replace(",",
										"")
									.Replace(".",
										"") ??
								"0", out long cVC) ? cVC : 0,
							Subscribers = rendererItem?["subscriberCountText"]?["simpleText"]?.ToString()
						});
						break;
					case "radioRenderer":
						items.Add(new RadioItem
						{
							Id = rendererItem?["playlistId"]
								?.ToString(),
							Title = rendererItem?["title"]?["simpleText"]
								?.ToString(),
							Thumbnails =
								(rendererItem?["thumbnails"]?[0]?["thumbnails"]?.ToObject<JArray>() ??
								 new JArray()).Select(Utils.ParseThumbnails).ToArray(),
							FirstVideoId = rendererItem?["navigationEndpoint"]?["watchEndpoint"]?["videoId"]
								?.ToString(),
							Channel = new Channel
							{
								Name = rendererItem?["longBylineText"]?["simpleText"]?.ToString(),
								Id = "",
								SubscriberCount = null,
								Avatars = null
							}
						});
						break;
					case "shelfRenderer":
						items.Add(new ShelfItem
						{
							Title = rendererItem?["title"]?["simpleText"]
								        ?.ToString() ??
							        rendererItem?["title"]?["runs"]?[0]?["text"]
								        ?.ToString(),
							Thumbnails = (rendererItem?["thumbnail"]?["thumbnails"]?.ToObject<JArray>() ??
							              new JArray()).Select(Utils.ParseThumbnails).ToArray(),
							Items = ParseRenderers(
								rendererItem?["content"]?["verticalListRenderer"]?["items"]
									?.ToObject<JArray>() ??
								rendererItem?["content"]?["horizontalListRenderer"]?["items"]
									?.ToObject<JArray>() ??
								rendererItem?["content"]?["expandedShelfContentsRenderer"]?["items"]
									?.ToObject<JArray>() ??
								new JArray()),
							CollapsedItemCount =
								rendererItem?["content"]?["verticalListRenderer"]?["collapsedItemCount"]
									?.ToObject<int>() ?? 0,
							Badges = ParseRenderers(rendererItem?["badges"]?.ToObject<JArray>() ?? new JArray())
								.Where(x => x is BadgeItem).Cast<BadgeItem>().ToArray(),
						});
						break;
					case "horizontalCardListRenderer":
						items.Add(new HorizontalCardListItem
						{
							Title = rendererItem?["header"]?["richListHeaderRenderer"]?["title"]?["simpleText"]
								?.ToString(),
							Items = ParseRenderers(rendererItem?["cards"]?.ToObject<JArray>() ?? new JArray())
						});
						break;
					case "searchRefinementCardRenderer":
						items.Add(new CardItem
						{
							Title = Utils.ReadRuns(rendererItem?["query"]?["runs"]?.ToObject<JArray>() ??
							                       new JArray()),
							Thumbnails = (rendererItem?["thumbnail"]?["thumbnails"]?.ToObject<JArray>() ??
							              new JArray()).Select(Utils.ParseThumbnails).ToArray()
						});
						break;
					case "compactVideoRenderer":
						items.Add(new VideoItem
						{
							Id = rendererItem?["videoId"]?.ToString(),
							Title = rendererItem?["title"]?["simpleText"]?.ToString(),
							Thumbnails =
								(rendererItem?["thumbnail"]?["thumbnails"]?.ToObject<JArray>() ??
								 new JArray()).Select(Utils.ParseThumbnails).ToArray(),
							UploadedAt = rendererItem?["publishedTimeText"]?["simpleText"]?.ToString(),
							Views = long.TryParse(
								rendererItem?["viewCountText"]?["simpleText"]?.ToString().Split(" ")[0]
									.Replace(",", "").Replace(".", "") ?? "0", out long cVV) ? cVV : 0,
							Channel = new Channel
							{
								Name = rendererItem?["longBylineText"]?["runs"]?[0]?["text"]?.ToString(),
								Id = rendererItem?["longBylineText"]?["runs"]?[0]?["navigationEndpoint"]?[
									"browseEndpoint"]?["browseId"]?.ToString(),
								SubscriberCount = null,
								Avatars = null
							},
							Duration = rendererItem?["thumbnailOverlays"]?[0]?[
								"thumbnailOverlayTimeStatusRenderer"]?["text"]?["simpleText"]?.ToString()
						});
						break;
					case "compactPlaylistRenderer":
						items.Add(new PlaylistItem
						{
							Id = rendererItem?["playlistId"]
								?.ToString(),
							Title = rendererItem?["title"]?["simpleText"]
								?.ToString(),
							Thumbnails =
								(rendererItem?["thumbnail"]?["thumbnails"]
									?.ToObject<JArray>() ?? new JArray()).Select(Utils.ParseThumbnails)
								.ToArray(),
							VideoCount = int.TryParse(
								rendererItem?["videoCountText"]?["runs"]?[0]?["text"]?.ToString().Replace(",", "")
									.Replace(".", "") ?? "0", out int cPVC) ? cPVC : 0,
							FirstVideoId = rendererItem?["navigationEndpoint"]?["watchEndpoint"]?["videoId"]
								?.ToString(),
							Channel = new Channel
							{
								Name = rendererItem?["longBylineText"]?["runs"]?[0]?["text"]
									?.ToString(),
								Id = rendererItem?["longBylineText"]?["runs"]?[0]?["navigationEndpoint"]?[
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
							Id = rendererItem?["playlistId"]
								?.ToString(),
							Title = rendererItem?["title"]?["simpleText"]
								?.ToString(),
							Thumbnails =
								(rendererItem?["thumbnail"]?["thumbnails"]
									?.ToObject<JArray>() ?? new JArray()).Select(Utils.ParseThumbnails)
								.ToArray(),
							FirstVideoId = rendererItem?["navigationEndpoint"]?["watchEndpoint"]?["videoId"]
								?.ToString(),
							Channel = new Channel
							{
								Name = rendererItem?["longBylineText"]?["simpleText"]?.ToString(),
								Id = "",
								SubscriberCount = null,
								Avatars = null
							}
						});
						break;
					case "continuationItemRenderer":
						items.Add(new ContinuationItem
						{
							Id = rendererItem?["continuationEndpoint"]?["continuationCommand"]?["token"]?.ToString()
						});
						break;
					case "playlistVideoRenderer":
						items.Add(new PlaylistVideoItem
						{
							Id = rendererItem?["videoId"]?.ToString(),
							Index = rendererItem?["index"]?["simpleText"]?.ToObject<long>() ?? 0,
							Title = Utils.ReadRuns(rendererItem?["title"]?["runs"]?.ToObject<JArray>() ??
							                       new JArray()),
							Thumbnails =
								(rendererItem?["thumbnail"]?["thumbnails"]?.ToObject<JArray>() ??
								 new JArray()).Select(Utils.ParseThumbnails).ToArray(),
							Channel = new Channel
							{
								Name = rendererItem?["shortBylineText"]?["runs"]?[0]?["text"]?.ToString(),
								Id = rendererItem?["shortBylineText"]?["runs"]?[0]?["navigationEndpoint"]?[
									"browseEndpoint"]?["browseId"]?.ToString(),
								SubscriberCount = null,
								Avatars = null
							},
							Duration = rendererItem?["lengthText"]?["simpleText"]?.ToString()
						});
						break;
					case "itemSectionRenderer":
						items.Add(new ItemSectionItem
						{
							Contents = ParseRenderers(rendererItem?["contents"]?.ToObject<JArray>() ?? new JArray())
						});
						break;
					case "gridRenderer":
						items.Add(new ItemSectionItem
						{
							Contents = ParseRenderers(rendererItem?["items"]?.ToObject<JArray>() ?? new JArray())
						});
						break;
					case "messageRenderer":
						items.Add(new MessageItem
						{
							Title = rendererItem?["text"]?["simpleText"]?.ToString()
						});
						break;
					case "channelAboutFullMetadataRenderer":
						items.Add(new ChannelAboutItem
						{
							Description = rendererItem?["description"]?["simpleText"]?.ToString(),
							Country = rendererItem?["country"]?["simpleText"]?.ToString(),
							Joined = Utils.ReadRuns(rendererItem?["joinedDateText"]?["runs"]?.ToObject<JArray>() ??
							                        new JArray()),
							ViewCount = rendererItem?["viewCountText"]?["simpleText"]?.ToString()
						});
						break;
					case "compactStationRenderer":
						items.Add(new StationItem
						{
							Id = rendererItem?["navigationEndpoint"]?["watchEndpoint"]?["playlistId"]?.ToString(),
							Title = rendererItem?["title"]?["simpleText"]?.ToString(),
							Thumbnails = 
								(rendererItem?["thumbnail"]?["thumbnails"]?.ToObject<JArray>() ??
								 new JArray()).Select(Utils.ParseThumbnails).ToArray(),
							VideoCount = rendererItem?["videoCountText"]?["runs"]?[0]?["text"].ToObject<int>() ?? 0,
							FirstVideoId = rendererItem?["navigationEndpoint"]?["watchEndpoint"]?["videoId"]?.ToString(),
							Description = rendererItem?["description"]?["simpleText"]?.ToString()
						});
						break;
					case "metadataBadgeRenderer":
						items.Add(new BadgeItem
						{
							Title = rendererItem?["label"]?.ToString(),
							Style = rendererItem?["style"]?.ToString()
						});
						break;
					case "promotedSparklesWebRenderer":
						// this is an ad
						// no one likes ads
						break;
					default:
						items.Add(new DynamicItem
						{
							Id = rendererName,
							Title = rendererItem?.ToString()
						});
						break;
				}
			}

			return items.ToArray();
		}
	}
}