using System;
using Newtonsoft.Json;

namespace YTProxy.Models
{
	public class YoutubePlayer
	{
		[JsonProperty("adaptive_formats")] public AdaptiveFormat[] AdaptiveFormats { get; set; }
		[JsonProperty("categories")] public string[] Categories { get; set; }
		[JsonProperty("channel")] public Channel Channel { get; set; }
		[JsonProperty("chapters")] public Chapter[] Chapters { get; set; }
		[JsonProperty("description")] public string Description { get; set; }
		[JsonProperty("duration")] public long? Duration { get; set; }
		[JsonProperty("engagement")] public Engagement Engagement { get; set; }
		[JsonProperty("formats")] public AdaptiveFormat[] Formats { get; set; }
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("live_status")] public string LiveStatus { get; set; }
		[JsonProperty("storyboards")] public AdaptiveFormat[] Storyboards { get; set; }
		[JsonProperty("subtitles")] public Subtitle[] Subtitles { get; set; }
		[JsonProperty("tags")] public string[] Tags { get; set; }
		[JsonProperty("thumbnails")] public Thumbnail[] Thumbnails { get; set; }
		[JsonProperty("title")] public string Title { get; set; }
		[JsonProperty("upload_date")] public string UploadDate { get; set; }
		[JsonProperty("recommended")] public ItemPreview[] Recommended { get; set; }
		[JsonProperty("error")] public string ErrorMessage { get; set; }

		public string GetHtmlDescription()
		{
			return Utils.GetHtmlDescription(Description);
		}

		public string GetMpdManifest(string proxyUrl)
		{
			return Utils.GetMpdManifest(this, proxyUrl);
		}
	}

	public class AdaptiveFormat
	{
		[JsonProperty("filesize")] public long? Filesize { get; set; }
		[JsonProperty("format")] public string Format { get; set; }
		[JsonProperty("format_id")] public string FormatId { get; set; }
		[JsonProperty("format_note")] public string FormatNote { get; set; }
		[JsonProperty("quality")] public long Quality { get; set; }
		[JsonProperty("resolution")] public string Resolution { get; set; }
		[JsonProperty("url")] public Uri Url { get; set; }
	}

	public class Channel
	{
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("name")] public string Name { get; set; }
		[JsonProperty("avatars")] public Thumbnail[] Avatars { get; set; }
	}

	public class Chapter
	{
		[JsonProperty("end_time")] public long EndTime { get; set; }
		[JsonProperty("start_time")] public long StartTime { get; set; }
		[JsonProperty("title")] public string Title { get; set; }
	}

	public class Engagement
	{
		[JsonProperty("dislikes")] public long Dislikes { get; set; }
		[JsonProperty("likes")] public long Likes { get; set; }
		[JsonProperty("views")] public long Views { get; set; }

		public float GetLikePercentage()
		{
			return Likes / (float)(Likes + Dislikes) * 100;
		}
	}

	public class Subtitle
	{
		[JsonProperty("ext")] public string Ext { get; set; }
		[JsonProperty("name")] public string Language { get; set; }
		[JsonProperty("url")] public Uri Url { get; set; }
	}

	public class Thumbnail
	{
		[JsonProperty("height")] public long Height { get; set; }
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("preference")] public long Preference { get; set; }
		[JsonProperty("resolution")] public string Resolution { get; set; }
		[JsonProperty("url")] public Uri Url { get; set; }
		[JsonProperty("width")] public long Width { get; set; }
	}
}