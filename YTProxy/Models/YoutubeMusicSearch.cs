using Newtonsoft.Json;

namespace YTProxy.Models
{
	public class YoutubeMusicSearch
	{
		[JsonProperty("best_result")] public ItemPreview BestResult { get; set; }
		[JsonProperty("songs")] public ItemContainer Songs { get; set; }
		[JsonProperty("albums")] public ItemContainer Albums { get; set; }
		[JsonProperty("artists")] public ItemContainer Artists { get; set; }
		[JsonProperty("videos")] public ItemContainer Videos { get; set; }
		[JsonProperty("community_playlists")] public ItemContainer CommunityPlaylists { get; set; }
	}

	public class ItemContainer
	{
		[JsonProperty("items")] public ItemPreview[] Items { get; set; }
		[JsonProperty("continuation_params")] public string ContinuationParams { get; set; }
	}
}