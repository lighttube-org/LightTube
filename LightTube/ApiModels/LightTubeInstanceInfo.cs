using Newtonsoft.Json;

namespace LightTube.ApiModels;

public class LightTubeInstanceInfo
{
	[JsonProperty("type")] public string Type { get; set; }
	[JsonProperty("version")] public string Version { get; set; }
	[JsonProperty("motd")] public string Motd { get; set; }
	[JsonProperty("allowsApi")] public bool AllowsApi { get; set; }
	[JsonProperty("allowsNewUsers")] public bool AllowsNewUsers { get; set; }
	[JsonProperty("allowsOauthApi")] public bool AllowsOauthApi { get; set; }
	[JsonProperty("allowsThirdPartyProxyUsage")] public bool AllowsThirdPartyProxyUsage { get; set; }
}