using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YTProxy.Models
{
	public class ItemPreview
	{
		[JsonProperty("type")] public string Type { get; set; }
		[JsonProperty("item")] private JObject ItemJson { get; set; }

		public Preview GetPreview()
		{
			return Type switch
			{
				"video" => JsonConvert.DeserializeObject<VideoPreview>(ItemJson.ToString()),
				"playlist" => JsonConvert.DeserializeObject<PlaylistPreview>(ItemJson.ToString()),
				"channel" => JsonConvert.DeserializeObject<ChannelPreview>(ItemJson.ToString()),
				"music-video" => JsonConvert.DeserializeObject<MusicVideoPreview>(ItemJson.ToString()),
				"music-song" => JsonConvert.DeserializeObject<MusicPreview>(ItemJson.ToString()),
				"music-album" => JsonConvert.DeserializeObject<AlbumPreview>(ItemJson.ToString()),
				"music-album-song" => JsonConvert.DeserializeObject<AlbumPreview>(ItemJson.ToString()),
				"music-artist" => JsonConvert.DeserializeObject<ArtistPreview>(ItemJson.ToString()),
				"music-playlist" => JsonConvert.DeserializeObject<MusicPlaylistPreview>(ItemJson.ToString()),
				var _ => JsonConvert.DeserializeObject<Preview>(ItemJson.ToString())
			};
		}
	}

	public class Preview
	{
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("title")] public string Title { get; set; }
		[JsonProperty("thumbnails", NullValueHandling = NullValueHandling.Ignore)] public Thumbnail[] Thumbnails { get; set; }
	}

	public class VideoPreview : Preview
	{
		[JsonProperty("uploaded_at")] public string UploadedAt { get; set; }
		[JsonProperty("views")] public long Views { get; set; }
		[JsonProperty("channel")] public Channel Channel { get; set; }
		[JsonProperty("duration")] public string Duration { get; set; }
	}

	public class PlaylistPreview : Preview
	{
		[JsonProperty("video_count")] public int VideoCount { get; set; }
		[JsonProperty("first_video_id")] public string FirstVideoId { get; set; }
		[JsonProperty("channel")] public Channel Channel { get; set; }
	}

	public class ChannelPreview : Preview
	{
		[JsonProperty("url")] public string Url { get; set; }
		[JsonProperty("description")] public string Description { get; set; }
		[JsonProperty("video_count")] public long VideoCount { get; set; }
		[JsonProperty("subscribers")] public string Subscribers { get; set; }
	}

	public class MusicVideoPreview : Preview
	{
		[JsonProperty("artist")] public Channel Channel { get; set; }
		[JsonProperty("duration")] public string Duration { get; set; }
	}

	public class MusicPreview : Preview
	{
		[JsonProperty("artist")] public Channel Channel { get; set; }
		[JsonProperty("album")] public Album Album { get; set; }
		[JsonProperty("duration")] public string Duration { get; set; }
	}

	public class AlbumPreview : Preview
	{
		[JsonProperty("type")] public string Type { get; set; }
		[JsonProperty("artist")] public Channel Channel { get; set; }
		[JsonProperty("year")] public int Year { get; set; }
	}

	public class AlbumSongPreview : Preview
	{
		[JsonProperty("index")] public int Index { get; set; }
		[JsonProperty("duration")] public string Duration { get; set; }
	}

	public class ArtistPreview : Preview
	{ }

	public class MusicPlaylistPreview : Preview
	{
		[JsonProperty("video_count")] public int VideoCount { get; set; }
		[JsonProperty("channel")] public Channel Channel { get; set; }
	}

	public class Album
	{
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("name")] public string Name { get; set; }
	}
}