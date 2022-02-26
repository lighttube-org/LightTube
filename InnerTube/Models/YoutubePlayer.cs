using System;
using Newtonsoft.Json;

namespace InnerTube.Models
{
	public class YoutubePlayer
	{
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("title")] public string Title { get; set; }
		[JsonProperty("description")] public string Description { get; set; }
		[JsonProperty("categories")] public string[] Categories { get; set; }
		[JsonProperty("tags")] public string[] Tags { get; set; }
		[JsonProperty("channel")] public Channel Channel { get; set; }
		[JsonProperty("upload_date")] public string UploadDate { get; set; }
		[JsonProperty("duration")] public long? Duration { get; set; }
		[JsonProperty("chapters")] public Chapter[] Chapters { get; set; }
		[JsonProperty("thumbnails")] public Thumbnail[] Thumbnails { get; set; }
		[JsonProperty("formats")] public Format[] Formats { get; set; }
		[JsonProperty("adaptive_formats")] public Format[] AdaptiveFormats { get; set; }
		[JsonProperty("subtitles")] public Subtitle[] Subtitles { get; set; }
		[JsonProperty("storyboards")] public Format[] Storyboards { get; set; }
		[JsonProperty("expires_in_seconds")] public string ExpiresInSeconds { get; set; }
		[JsonProperty("error")] public string ErrorMessage { get; set; }

		public string GetHtmlDescription()
		{
			return Utils.GetHtmlDescription(Description);
		}
	}

	public class Chapter
	{
		[JsonProperty("title")] public string Title { get; set; }
		[JsonProperty("start_time")] public long StartTime { get; set; }
		[JsonProperty("end_time")] public long EndTime { get; set; }
	}

	public class Format
	{
		[JsonProperty("format")] public string FormatName { get; set; }
		[JsonProperty("format_id")] public string FormatId { get; set; }
		[JsonProperty("format_note")] public string FormatNote { get; set; }
		[JsonProperty("filesize")] public long? Filesize { get; set; }
		[JsonProperty("quality")] public long Quality { get; set; }
		[JsonProperty("bitrate")] public double Bitrate { get; set; }
		[JsonProperty("audio_codec")] public string AudioCodec { get; set; }
		[JsonProperty("video_codec")] public string VideoCodec { get; set; }
		[JsonProperty("audio_sample_rate")] public long? AudioSampleRate { get; set; }
		[JsonProperty("resolution")] public string Resolution { get; set; }
		[JsonProperty("url")] public Uri Url { get; set; }
		[JsonProperty("init_range")] public Range InitRange { get; set; }
		[JsonProperty("index_range")] public Range IndexRange { get; set; }
	}

	public class Range
	{
		[JsonProperty("start")] public string Start { get; set; }
		[JsonProperty("end")] public string End { get; set; }
		
		public Range(string start, string end)
		{
			Start = start;
			End = end;
		}
	}

	public class Channel
	{
		[JsonProperty("name")] public string Name { get; set; }
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("avatars")] public Thumbnail[] Avatars { get; set; }
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
		[JsonProperty("url")] public Uri Url { get; set; }
		[JsonProperty("width")] public long Width { get; set; }
	}
}