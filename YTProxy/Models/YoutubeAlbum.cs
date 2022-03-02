using Newtonsoft.Json;

namespace YTProxy.Models
{
	public class YoutubeAlbum
	{
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("name")] public string Name { get; set; }
		[JsonProperty("type")] public string Type { get; set; }
		[JsonProperty("song_count")] public long SongCount { get; set; }
		[JsonProperty("year")] public long Year { get; set; }
		[JsonProperty("duration")] public string Duration { get; set; }
		[JsonProperty("artist")] public Channel Artist { get; set; }
		[JsonProperty("album_art")] public Thumbnail[] AlbumArt { get; set; }
		[JsonProperty("songs")] public ItemPreview[] Songs { get; set; }
	}
}