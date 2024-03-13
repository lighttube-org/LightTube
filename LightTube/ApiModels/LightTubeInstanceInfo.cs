using Newtonsoft.Json;

namespace LightTube.ApiModels;

public class LightTubeInstanceInfo
{
	[JsonProperty("type")] public string Type { get; set; }
	[JsonProperty("version")] public string Version { get; set; }
	[JsonProperty("motd")] public string[] Messages { get; set; }
	[JsonProperty("alert")] public string? Alert { get; set; }
	[JsonProperty("config")] public Dictionary<string, object> Config { get; set; }
}