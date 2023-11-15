using Newtonsoft.Json;

namespace LightTube;

public class LightTubeExport
{
	[JsonProperty("type")] public string Type { get; set; }
	[JsonProperty("host")] public string Host { get; set; }
	[JsonProperty("subscriptions")] public string[] Subscriptions { get; set; }
	[JsonProperty("playlists")] public ImportedData.Playlist[] Playlists { get; set; }
}