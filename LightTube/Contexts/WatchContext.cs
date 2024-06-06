using InnerTube.Models;
using LightTube.Database;
using LightTube.Database.Models;

namespace LightTube.Contexts;

public class WatchContext : BaseContext
{
	public PlayerContext Player;
	public InnerTubeVideo Video;
	public VideoPlaylistInfo? Playlist;
	public ContinuationResponse? Comments;
	public long Dislikes;
	public long Likes;
	public SponsorBlockSegment[] Sponsors;

	public WatchContext(HttpContext context, InnerTubePlayer innerTubePlayer, InnerTubeVideo innerTubeVideo,
		ContinuationResponse? comments, bool compatibility, int dislikes,
		SponsorBlockSegment[] sponsors) : base(context)
	{
		Player = new PlayerContext(context, innerTubePlayer, innerTubeVideo, "embed", compatibility,
			context.Request.Query["q"], sponsors);
		Video = innerTubeVideo;
		Playlist = Video.Playlist;
		Comments = comments;
		Dislikes = dislikes;
		Likes = innerTubeVideo.LikeCount;
		Sponsors = sponsors;
		GuideHidden = true;

		AddMeta("description", Video.Description);
		AddMeta("author", Video.Channel.Title);
		AddMeta("og:title", Video.Title);
		AddMeta("og:description", Video.Description);
		AddMeta("og:url",
			$"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
		AddMeta("og:image", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("og:video:url", $"https://{context.Request.Host}/proxy/media/{Video.Id}/18.mp4");
		AddMeta("og:video:width", "640");
		AddMeta("og:video:height", "360");
		AddMeta("og:type", "video.other");
		AddMeta("twitter:card", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("twitter:player", $"https://{context.Request.Host}/embed/{Video.Id}");
		AddMeta("twitter:player:stream", $"https://{context.Request.Host}/proxy/media/{Video.Id}/18.mp4");

		AddStylesheet("/lib/ltplayer.css");

		AddScript("/lib/ltplayer.js");
		AddScript("/lib/hls.js");
		AddScript("/js/player.js");
	}

	public WatchContext(HttpContext context, Exception e, InnerTubeVideo innerTubeVideo, ContinuationResponse? comments,
		int dislikes) : base(context)
	{
		Player = new PlayerContext(context, e);
		Video = innerTubeVideo;
		Playlist = Video.Playlist;
		Comments = comments;
		Dislikes = dislikes;
		Likes = innerTubeVideo.LikeCount;
		Sponsors = [];
		GuideHidden = true;

		AddMeta("description", Video.Description);
		AddMeta("author", Video.Channel.Title);
		AddMeta("og:title", Video.Title);
		AddMeta("og:description", Video.Description);
		AddMeta("og:url",
			$"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
		AddMeta("og:image", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("og:video:url", $"https://{context.Request.Host}/proxy/media/{Video.Id}/18.mp4");
		AddMeta("og:video:width", "640");
		AddMeta("og:video:height", "360");
		AddMeta("og:type", "video.other");
		AddMeta("twitter:card", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("twitter:player", $"https://{context.Request.Host}/embed/{Video.Id}");
		AddMeta("twitter:player:stream", $"https://{context.Request.Host}/proxy/media/{Video.Id}/18.mp4");
	}

	public WatchContext(HttpContext context, InnerTubePlayer innerTubePlayer, InnerTubeVideo innerTubeVideo,
		DatabasePlaylist? playlist, ContinuationResponse? comments, bool compatibility, int dislikes,
		SponsorBlockSegment[] sponsors) : base(context)
	{
		Player = new PlayerContext(context, innerTubePlayer, innerTubeVideo, "embed", compatibility,
			context.Request.Query["q"], sponsors);
		Video = innerTubeVideo;
		Playlist = playlist?.GetVideoPlaylistInfo(innerTubeVideo.Id,
			DatabaseManager.Users.GetUserFromId(playlist.Author).Result!,
			DatabaseManager.Playlists.GetPlaylistVideos(playlist.Id, Localization),
			Localization);
		if (playlist != null && playlist.Visibility == PlaylistVisibility.Private)
			if (playlist.Author != User?.UserID)
				Playlist = null;
		Comments = comments;
		Dislikes = dislikes;
		Likes = innerTubeVideo.LikeCount;
		Sponsors = sponsors;
		GuideHidden = true;

		AddMeta("description", Video.Description);
		AddMeta("author", Video.Channel.Title);
		AddMeta("og:title", Video.Title);
		AddMeta("og:description", Video.Description);
		AddMeta("og:url",
			$"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
		AddMeta("og:image", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("og:video:url", $"https://{context.Request.Host}/proxy/media/{Video.Id}/18.mp4");
		AddMeta("og:video:width", "640");
		AddMeta("og:video:height", "360");
		AddMeta("og:type", "video.other");
		AddMeta("twitter:card", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("twitter:player", $"https://{context.Request.Host}/embed/{Video.Id}");
		AddMeta("twitter:player:stream", $"https://{context.Request.Host}/proxy/media/{Video.Id}/18.mp4");

		AddStylesheet("/lib/ltplayer.css");

		AddScript("/lib/ltplayer.js");
		AddScript("/lib/hls.js");
		AddScript("/js/player.js");
	}

	public WatchContext(HttpContext context, Exception e, InnerTubeVideo innerTubeVideo, DatabasePlaylist? playlist,
		ContinuationResponse? comments, int dislikes) : base(context)
	{
		Player = new PlayerContext(context, e);
		Video = innerTubeVideo;
		Playlist = playlist?.GetVideoPlaylistInfo(innerTubeVideo.Id,
			DatabaseManager.Users.GetUserFromId(playlist.Author).Result!,
			DatabaseManager.Playlists.GetPlaylistVideos(playlist.Id, Localization),
			Localization);
		if (playlist != null && playlist.Visibility == PlaylistVisibility.Private)
			if (playlist.Author != User?.UserID)
				Playlist = null;
		Comments = comments;
		Dislikes = dislikes;
		Likes = innerTubeVideo.LikeCount;
		Sponsors = [];
		GuideHidden = true;

		AddMeta("description", Video.Description);
		AddMeta("author", Video.Channel.Title);
		AddMeta("og:title", Video.Title);
		AddMeta("og:description", Video.Description);
		AddMeta("og:url",
			$"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
		AddMeta("og:image", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("og:video:url", $"https://{context.Request.Host}/proxy/media/{Video.Id}/18.mp4");
		AddMeta("og:video:width", "640");
		AddMeta("og:video:height", "360");
		AddMeta("og:type", "video.other");
		AddMeta("twitter:card", $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{Video.Id}/-1");
		AddMeta("twitter:player", $"https://{context.Request.Host}/embed/{Video.Id}");
		AddMeta("twitter:player:stream", $"https://{context.Request.Host}/proxy/media/{Video.Id}/18.mp4");
	}
}