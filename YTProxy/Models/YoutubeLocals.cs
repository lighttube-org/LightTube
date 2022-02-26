using System.Collections.Generic;
using Newtonsoft.Json;

namespace YTProxy.Models
{
	public class YoutubeLocals
	{
		[JsonProperty("languages")] public Dictionary<string, string> Languages { get; set; }
		[JsonProperty("regions")] public Dictionary<string, string> Regions { get; set; }
	}
}