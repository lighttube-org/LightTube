using Newtonsoft.Json;

namespace YTProxy.Models
{
	public class YoutubeSearch
	{
		[JsonProperty("refinements")]
		public string[] Refinements { get; set; }

		[JsonProperty("estimated_results")]
		public long EstimatedResults { get; set; }

		[JsonProperty("results")]
		public ItemPreview[] Results { get; set; }

		[JsonProperty("continuationKey")]
		public string ContinuationKey { get; set; }
	}
}