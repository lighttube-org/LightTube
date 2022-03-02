using Newtonsoft.Json;

namespace YTProxy.Models
{
	public class YoutubeMusicMoreSearch
	{
		[JsonProperty("items")] public ItemPreview[] Items { get; set; }
		[JsonProperty("continuation_key")] public string ContinuationParams { get; set; }
	}
}