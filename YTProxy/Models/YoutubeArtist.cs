using Newtonsoft.Json;

namespace YTProxy.Models
{
	public class YoutubeArtist
	{
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("name")] public string Name { get; set; }
		[JsonProperty("description")] public string Description { get; set; }
		[JsonProperty("avatars")] public Thumbnail[] Avatars { get; set; }
		[JsonProperty("songs")] public ContinuationItemContainer Songs { get; set; }
		[JsonProperty("albums")] public ContinuationItemContainer Albums { get; set; }
		[JsonProperty("singles")] public ContinuationItemContainer Singles { get; set; }
		[JsonProperty("videos")] public ContinuationItemContainer Videos { get; set; }
		[JsonProperty("might_like")] public ItemPreview[] MightLike { get; set; }
		[JsonProperty("featured_on")] public ItemPreview[] FeaturedOn { get; set; }
	}

	public class ContinuationItemContainer
	{
		[JsonProperty("items")] public ItemPreview[] Items { get; set; }
		[JsonProperty("more_params")] public ContinuationParams MoreParams { get; set; }
	}

	public class ContinuationParams
	{
		[JsonProperty("browseId")] public string BrowseId { get; set; }
		[JsonProperty("params")] public string Params { get; set; }
	}
}