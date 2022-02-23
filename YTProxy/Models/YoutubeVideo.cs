using System;
using Newtonsoft.Json;

namespace YTProxy.Models
{
	public class YoutubeVideo
	{
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("title")] public string Title { get; set; }
		[JsonProperty("description")] public string Description { get; set; }
		[JsonProperty("channel")] public Channel Channel { get; set; }
		[JsonProperty("upload_date")] public string UploadDate { get; set; }
		[JsonProperty("engagement")] public Engagement Engagement { get; set; }
		[JsonProperty("thumbnails")] public Thumbnail[] Thumbnails { get; set; }
		[JsonProperty("recommended")] public ItemPreview[] Recommended { get; set; }
	}

	public class Avatar
	{
		[JsonProperty("url")] public Uri Url { get; set; }
		[JsonProperty("width")] public long Width { get; set; }
		[JsonProperty("height")] public long Height { get; set; }
	}

	public class Engagement
	{
		[JsonProperty("likes")] public long Likes { get; set; }
		[JsonProperty("dislikes")] public long Dislikes { get; set; }
		[JsonProperty("views")] public long Views { get; set; }
		
		public float GetLikePercentage()
		{
			return Likes / (float)(Likes + Dislikes) * 100;
		}
	}
}