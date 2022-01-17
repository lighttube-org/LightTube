using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YTProxy.Models
{
	public class ItemPreview
	{
		[JsonProperty("type")] public string Type { get; set; }
		[JsonProperty("item")] private JObject ItemJson { get; set; }

		public Preview GetPreview()
		{
			switch (Type)
			{
				case "video":
					return JsonConvert.DeserializeObject<VideoPreview>(ItemJson.ToString());
				case "playlist":
					return JsonConvert.DeserializeObject<PlaylistPreview>(ItemJson.ToString());
				case "channel":
					return JsonConvert.DeserializeObject<ChannelPreview>(ItemJson.ToString());
				default:
					return JsonConvert.DeserializeObject<Preview>(ItemJson.ToString());
			}
		}
	}

	public class Preview
	{
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("title")] public string Title { get; set; }
		[JsonProperty("thumbnails")] public Thumbnail[] Thumbnails { get; set; }
	}

	public class VideoPreview : Preview
	{
		[JsonProperty("uploaded_at")] public string UploadedAt { get; set; }
		[JsonProperty("views")] public long Views { get; set; }
		[JsonProperty("channel")] public Channel Channel { get; set; }
		[JsonProperty("duration")] public string Duration { get; set; }
	}

	public class PlaylistPreview : Preview
	{
		[JsonProperty("video_count")] public int VideoCount { get; set; }
		[JsonProperty("first_video_id")] public string FirstVideoId { get; set; }
		[JsonProperty("channel")] public Channel Channel { get; set; }
	}

	public class ChannelPreview : Preview
	{
		[JsonProperty("url")] public string Url { get; set; }
		[JsonProperty("description")] public string Description { get; set; }
		[JsonProperty("video_count")] public long VideoCount { get; set; }
		[JsonProperty("subscribers")] public string Subscribers { get; set; }
	}
}