using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InnerTube.Models.YtDlp
{
	public class YtDlpOutput
	{
		private static string[] MuxedFormats =
		{
			"17", "18", "59", "22", "37"
		};

		private static HttpClient Client = new();

		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("title")] public string Title { get; set; }
		[JsonProperty("formats")] public Format[] Formats { get; set; }
		[JsonProperty("thumbnails")] public Thumbnail[] Thumbnails { get; set; }
		[JsonProperty("thumbnail")] public Uri Thumbnail { get; set; }
		[JsonProperty("description")] public string Description { get; set; }
		[JsonProperty("upload_date")] public string UploadDate { get; set; }
		[JsonProperty("uploader")] public string Uploader { get; set; }
		[JsonProperty("uploader_id")] public string UploaderId { get; set; }
		[JsonProperty("uploader_url")] public Uri UploaderUrl { get; set; }
		[JsonProperty("channel_id")] public string ChannelId { get; set; }
		[JsonProperty("channel_url")] public Uri ChannelUrl { get; set; }
		[JsonProperty("duration")] public long Duration { get; set; }
		[JsonProperty("view_count")] public long ViewCount { get; set; }
		[JsonProperty("average_rating")] public object AverageRating { get; set; }
		[JsonProperty("age_limit")] public long AgeLimit { get; set; }
		[JsonProperty("webpage_url")] public Uri WebpageUrl { get; set; }
		[JsonProperty("categories")] public string[] Categories { get; set; }
		[JsonProperty("tags")] public string[] Tags { get; set; }
		[JsonProperty("playable_in_embed")] public bool PlayableInEmbed { get; set; }
		[JsonProperty("is_live")] public bool IsLive { get; set; }
		[JsonProperty("was_live")] public bool WasLive { get; set; }
		[JsonProperty("live_status")] public string LiveStatus { get; set; }
		[JsonProperty("release_timestamp")] public object ReleaseTimestamp { get; set; }
		[JsonProperty("automatic_captions")] public Dictionary<string, Subtitle[]> AutomaticCaptions { get; set; }
		[JsonProperty("subtitles")] public Dictionary<string, Subtitle[]> Subtitles { get; set; }
		[JsonProperty("chapters")] public Chapter[] Chapters { get; set; }
		[JsonProperty("like_count")] public long LikeCount { get; set; }
		[JsonProperty("channel")] public string Channel { get; set; }
		[JsonProperty("availability")] public string Availability { get; set; }
		[JsonProperty("original_url")] public Uri OriginalUrl { get; set; }
		[JsonProperty("webpage_url_basename")] public string WebpageUrlBasename { get; set; }
		[JsonProperty("webpage_url_domain")] public string WebpageUrlDomain { get; set; }
		[JsonProperty("extractor")] public string Extractor { get; set; }
		[JsonProperty("extractor_key")] public string ExtractorKey { get; set; }
		[JsonProperty("playlist")] public object Playlist { get; set; }
		[JsonProperty("playlist_index")] public object PlaylistIndex { get; set; }
		[JsonProperty("display_id")] public string DisplayId { get; set; }
		[JsonProperty("duration_string")] public string DurationString { get; set; }
		[JsonProperty("requested_subtitles")] public object RequestedSubtitles { get; set; }
		[JsonProperty("__has_drm")] public bool HasDrm { get; set; }
		[JsonProperty("requested_formats")] public Format[] RequestedFormats { get; set; }
		[JsonProperty("format")] public string Format { get; set; }
		[JsonProperty("format_id")] public string FormatId { get; set; }
		[JsonProperty("ext")] public string Ext { get; set; }
		[JsonProperty("protocol")] public string Protocol { get; set; }
		[JsonProperty("language")] public object Language { get; set; }
		[JsonProperty("format_note")] public string FormatNote { get; set; }
		[JsonProperty("filesize_approx")] public long FilesizeApprox { get; set; }
		[JsonProperty("tbr")] public double Tbr { get; set; }
		[JsonProperty("width")] public long Width { get; set; }
		[JsonProperty("height")] public long Height { get; set; }
		[JsonProperty("resolution")] public string Resolution { get; set; }
		[JsonProperty("fps")] public long Fps { get; set; }
		[JsonProperty("dynamic_range")] public string DynamicRange { get; set; }
		[JsonProperty("vcodec")] public string Vcodec { get; set; }
		[JsonProperty("vbr")] public double Vbr { get; set; }
		[JsonProperty("stretched_ratio")] public object StretchedRatio { get; set; }
		[JsonProperty("acodec")] public string Acodec { get; set; }
		[JsonProperty("abr")] public double Abr { get; set; }
		[JsonProperty("asr")] public long Asr { get; set; }
		[JsonProperty("epoch")] public long Epoch { get; set; }

		public async Task<YoutubePlayer> GetYoutubePlayer()
		{
			HttpRequestMessage hrm = new(HttpMethod.Post,
				"https://www.youtube.com/youtubei/v1/player?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8");
			
			byte[] buffer = Encoding.UTF8.GetBytes(
				RequestContext.BuildRequestContextJson(new Dictionary<string, object>
				{
					["videoId"] = Id
				}));
			ByteArrayContent byteContent = new(buffer);
			byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			hrm.Content = byteContent;
			HttpResponseMessage ytPlayerRequest = await Client.SendAsync(hrm);
			JObject ytPlayer = JObject.Parse(await ytPlayerRequest.Content.ReadAsStringAsync());
			Dictionary<string, (Range InitRange, Range IndexRange)> ranges = new();

			// ReSharper disable once PossibleNullReferenceException
			foreach (JToken format in ytPlayer["streamingData"]?["adaptiveFormats"])
			{
				Range initRange = new(format?["initRange"]?["start"]?.ToString(),
					format?["initRange"]?["end"]?.ToString());
				Range indexRange = new(format?["indexRange"]?["start"]?.ToString(),
					format?["indexRange"]?["end"]?.ToString());

				ranges.Add(format?["itag"]?.ToString() ?? string.Empty, (initRange, indexRange));
			}

			YoutubePlayer player = new()
			{
				Id = Id,
				Title = Title,
				Description = Description,
				Categories = Categories,
				Tags = Tags,
				Channel = new Channel
				{
					Avatars = Array.Empty<Models.Thumbnail>(),
					Id = ChannelId,
					Name = Channel
				},
				UploadDate = UploadDate[..4] + "-" + UploadDate[4..6] + "-" + UploadDate[6..8],
				Duration = Duration
			};
			try
			{
				player.Chapters = Chapters.Select(x => new Models.Chapter
				{
					Title = x.Title,
					StartTime = x.StartTime,
					EndTime = x.EndTime
				}).ToArray();
			}
			catch
			{
				player.Chapters = Array.Empty<Models.Chapter>();
			}

			player.Thumbnails = Thumbnails.Where(x => x.Height.HasValue && x.Width.HasValue).Select(x =>
				new Models.Thumbnail
				{
					Height = x.Height ?? 0,
					Url = x.Url,
					Width = x.Width ?? 0
				}).ToArray();

			player.Storyboards = Array.Empty<Models.Format>();
			player.Formats = Array.Empty<Models.Format>();
			player.AdaptiveFormats = Array.Empty<Models.Format>();
			foreach (Format format in Formats)
			{
				ranges.TryGetValue(format.FormatId, out (Range InitRange, Range IndexRange) range);
				Models.Format f = new()
				{
					FormatName = format.FormatName,
					FormatId = format.FormatId,
					FormatNote = format.FormatNote,
					Filesize = format.Filesize,
					Quality = format.Quality ?? 0,
					Bitrate = format.TotalBitrate ?? 0,
					AudioCodec = format.AudioCodec,
					VideoCodec = format.VideoCodec,
					AudioSampleRate = format.AudioSampleRate,
					Resolution = format.Resolution,
					Url = format.Url,
					InitRange = range.InitRange,
					IndexRange = range.IndexRange
				};
				if (format.FormatNote == "storyboard")
					player.Storyboards = player.Storyboards.Append(f).ToArray();
				else if (MuxedFormats.Contains(format.FormatId))
					player.Formats = player.Formats.Append(f).ToArray();
				else
					player.AdaptiveFormats = player.AdaptiveFormats.Append(f).ToArray();
			}
			
			player.Subtitles = Subtitles
				.Select(x => x.Value.FirstOrDefault(s => s.Ext == "vtt"))
				.Where(x => x != null)
				.Select(x => new Models.Subtitle
				{
					Ext = x.Ext,
					Language = x.Name,
					Url = x.Url
				})
				.ToArray();
			player.ExpiresInSeconds = ytPlayer["streamingData"]?["expiresInSeconds"]?.ToString();
			player.ErrorMessage = null;
			return player;
		}
	}

	public class Chapter
	{
		[JsonProperty("title")] public string Title { get; set; }
		[JsonProperty("start_time")] public long StartTime { get; set; }
		[JsonProperty("end_time")] public long EndTime { get; set; }
	}

	public class Subtitle
	{
		[JsonProperty("ext")] public string Ext { get; set; }
		[JsonProperty("url")] public Uri Url { get; set; }
		[JsonProperty("name")] public string Name { get; set; }
	}

	public class Format
	{
		[JsonProperty("format_id")] public string FormatId { get; set; }
		[JsonProperty("format_note")] public string FormatNote { get; set; }
		[JsonProperty("ext")] public string Extension { get; set; }
		[JsonProperty("protocol")] public string Protocol { get; set; }
		[JsonProperty("acodec")] public string AudioCodec { get; set; }
		[JsonProperty("vcodec")] public string VideoCodec { get; set; }
		[JsonProperty("url")] public Uri Url { get; set; }
		[JsonProperty("width")] public long? Width { get; set; }
		[JsonProperty("height")] public long? Height { get; set; }

		[JsonProperty("fragments", NullValueHandling = NullValueHandling.Ignore)]
		public Fragment[] Fragments { get; set; }

		[JsonProperty("audio_ext")] public string AudioExt { get; set; }
		[JsonProperty("video_ext")] public string VideoExt { get; set; }
		[JsonProperty("format")] public string FormatName { get; set; }
		[JsonProperty("resolution")] public string Resolution { get; set; }
		[JsonProperty("http_headers")] public HttpHeaders HttpHeaders { get; set; }
		[JsonProperty("asr")] public long? AudioSampleRate { get; set; }

		[JsonProperty("filesize", NullValueHandling = NullValueHandling.Ignore)]
		public long? Filesize { get; set; }

		[JsonProperty("source_preference", NullValueHandling = NullValueHandling.Ignore)]
		public long? SourcePreference { get; set; }

		[JsonProperty("fps")] public long? Fps { get; set; }

		[JsonProperty("quality", NullValueHandling = NullValueHandling.Ignore)]
		public long? Quality { get; set; }

		[JsonProperty("tbr", NullValueHandling = NullValueHandling.Ignore)]
		public double? TotalBitrate { get; set; }

		[JsonProperty("language", NullValueHandling = NullValueHandling.Ignore)]
		public string Language { get; set; }

		[JsonProperty("language_preference", NullValueHandling = NullValueHandling.Ignore)]
		public long? LanguagePreference { get; set; }

		[JsonProperty("dynamic_range")] public string DynamicRange { get; set; }

		[JsonProperty("abr", NullValueHandling = NullValueHandling.Ignore)]
		public double? AudioBitrate { get; set; }

		[JsonProperty("downloader_options", NullValueHandling = NullValueHandling.Ignore)]
		public DownloaderOptions DownloaderOptions { get; set; }

		[JsonProperty("container", NullValueHandling = NullValueHandling.Ignore)]
		public string Container { get; set; }

		[JsonProperty("vbr", NullValueHandling = NullValueHandling.Ignore)]
		public double? VideoBitrate { get; set; }
	}

	public class DownloaderOptions
	{
		[JsonProperty("http_chunk_size")] public long HttpChunkSize { get; set; }
	}

	public class Fragment
	{
		[JsonProperty("path")] public Uri Path { get; set; }
		[JsonProperty("duration")] public long Duration { get; set; }
	}

	public class HttpHeaders
	{
		[JsonProperty("User-Agent")] public string UserAgent { get; set; }
		[JsonProperty("Accept")] public string Accept { get; set; }
		[JsonProperty("Accept-Encoding")] public string AcceptEncoding { get; set; }
		[JsonProperty("Accept-Language")] public string AcceptLanguage { get; set; }
	}

	public class Thumbnail
	{
		[JsonProperty("url")] public Uri Url { get; set; }
		[JsonProperty("preference")] public long Preference { get; set; }
		[JsonProperty("id")] public string Id { get; set; }

		[JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
		public long? Height { get; set; }

		[JsonProperty("width", NullValueHandling = NullValueHandling.Ignore)]
		public long? Width { get; set; }

		[JsonProperty("resolution", NullValueHandling = NullValueHandling.Ignore)]
		public string Resolution { get; set; }
	}
}