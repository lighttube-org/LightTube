using Newtonsoft.Json;

namespace YTProxy.Models
{
	public class YoutubeChannel
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("url")]
		public string Url { get; set; }

		[JsonProperty("avatars")]
		public Thumbnail[] Avatars { get; set; }

		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("videos")]
		public VideoPreview[] Videos { get; set; }

		[JsonProperty("subscribers")]
		public string Subscribers { get; set; }

		[JsonProperty("continuation_token")]
		public string ContinuationToken { get; set; }

		public string GetHtmlDescription()
		{
			return Utils.GetHtmlDescription(Description);
		}
	}
}