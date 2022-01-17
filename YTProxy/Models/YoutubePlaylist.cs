using Newtonsoft.Json;

namespace YTProxy.Models
{
	public class YoutubePlaylist
	{
		[JsonProperty("title")] public string Title { get; set; }

		[JsonProperty("description")] public string Description { get; set; }

		[JsonProperty("video_count")] public string VideoCount { get; set; }

		[JsonProperty("view_count")] public string ViewCount { get; set; }

		[JsonProperty("last_updated")] public string LastUpdated { get; set; }

		[JsonProperty("thumbnail")] public Thumbnail[] Thumbnail { get; set; }

		[JsonProperty("channel")] public Channel Channel { get; set; }

		[JsonProperty("videos")] public VideoPreview[] Videos { get; set; }

		[JsonProperty("continuation_token")] public string ContinuationToken { get; set; }
	}
}