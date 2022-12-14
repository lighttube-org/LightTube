using InnerTube;
using Newtonsoft.Json.Linq;

namespace LightTube.Database.Models;

public class DatabasePlaylist
{
	private const string INNERTUBE_PLAYLIST_INFO_TEMPLATE = "{\"playlistId\":\"%%PLAYLIST_ID%%\",\"title\":\"%%TITLE%%\",\"totalVideos\":%%VIDEO_COUNT%%,\"currentIndex\":%%CURRENT_INDEX%%,\"localCurrentIndex\":%%CURRENT_INDEX%%,\"longBylineText\":{\"runs\":[{\"text\":\"%%CHANNEL_TITLE%%\",\"navigationEndpoint\":{\"browseEndpoint\":{\"browseId\":\"%%CHANNEL_ID%%\"}}}]},\"isInfinite\":false,\"isCourse\":false,\"ownerBadges\":[],\"contents\":[%%CONTENTS%%]}";
	public string Id;
	public string Name;
	public string Description;
	public PlaylistVisibility Visibility;
	public List<string> VideoIds;
	public string Author;
	public DateTimeOffset LastUpdated;

	public InnerTubePlaylistInfo? GetInnerTubePlaylistInfo(string currentVideoId)
	{
		string json = INNERTUBE_PLAYLIST_INFO_TEMPLATE
			.Replace("%%PLAYLIST_ID%%", Id)
			.Replace("%%TITLE%%", Name)
			.Replace("%%VIDEO_COUNT%%", VideoIds.Count.ToString())
			.Replace("%%CURRENT_INDEX%%", VideoIds.IndexOf(currentVideoId).ToString())
			.Replace("%%CHANNEL_TITLE%%", Author)
			.Replace("%%CHANNEL_ID%%", DatabaseManager.Users.GetUserFromId(Author).Result?.LTChannelID)
			.Replace("%%CONTENTS%%", DatabaseManager.Playlists.GetPlaylistPanelVideosJson(Id, currentVideoId));
		return new InnerTubePlaylistInfo(JObject.Parse(json));
	}
}

public enum PlaylistVisibility
{
	PRIVATE,
	UNLISTED,
	VISIBLE
}