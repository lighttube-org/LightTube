using InnerTube;
using InnerTube.Renderers;
using LightTube.Database;
using LightTube.Database.Models;

namespace LightTube.Contexts;

public class PlaylistContext : BaseContext
{
    public string Id;
    public string PlaylistThumbnail;
    public string PlaylistTitle;
    public string PlaylistDescription;
    public string AuthorName;
    public string AuthorId;
    public string ViewCountText;
    public string LastUpdatedText;
    public bool Editable;
    public IEnumerable<IRenderer> Items;
    public int? Continuation;

    public PlaylistContext(HttpContext context, InnerTubePlaylist playlist) : base(context)
    {
        Id = playlist.Id;
        PlaylistThumbnail = playlist.Sidebar.Thumbnails.Last().Url.ToString();
        PlaylistTitle = playlist.Sidebar.Title;
        PlaylistDescription = playlist.Sidebar.Description;
        AuthorName = playlist.Sidebar.Channel.Title;
        AuthorId = playlist.Sidebar.Channel.Id!;
        ViewCountText = playlist.Sidebar.ViewCountText;
        LastUpdatedText = playlist.Sidebar.LastUpdated;
        Editable = false;
        Items = playlist.Videos;
        Continuation = playlist.Continuation?.ContinueFrom;

        AddMeta("description", playlist.Sidebar.Description);
        AddMeta("author", playlist.Sidebar.Title);
        AddMeta("og:title", playlist.Sidebar.Title);
        AddMeta("og:description", playlist.Sidebar.Description);
        AddMeta("og:url",
            $"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
        AddMeta("og:image",
            $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{playlist.Videos.First().Id}/-1");
        AddMeta("twitter:card",
            $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{playlist.Videos.First().Id}/-1");
    }

    public PlaylistContext(HttpContext context, InnerTubePlaylist playlist, InnerTubeContinuationResponse continuation)
        : base(context)
    {
        Id = playlist.Id;
        PlaylistThumbnail = playlist.Sidebar.Thumbnails.Last().Url.ToString();
        PlaylistTitle = playlist.Sidebar.Title;
        PlaylistDescription = playlist.Sidebar.Description;
        AuthorName = playlist.Sidebar.Channel.Title;
        AuthorId = playlist.Sidebar.Channel.Id!;
        ViewCountText = playlist.Sidebar.ViewCountText;
        LastUpdatedText = playlist.Sidebar.LastUpdated;
        Editable = false;
        Items = continuation.Contents;
        Continuation = continuation.Continuation is not null
            ? InnerTube.Utils.UnpackPlaylistContinuation(continuation.Continuation).ContinueFrom
            : null;

        AddMeta("description", playlist.Sidebar.Description);
        AddMeta("author", playlist.Sidebar.Title);
        AddMeta("og:title", playlist.Sidebar.Title);
        AddMeta("og:description", playlist.Sidebar.Description);
        AddMeta("og:url",
            $"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
        AddMeta("og:image",
            $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{playlist.Videos.First().Id}/-1");
        AddMeta("twitter:card",
            $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{playlist.Videos.First().Id}/-1");
    }

    public PlaylistContext(HttpContext context, DatabasePlaylist? playlist) : base(context)
    {
        bool visible = (playlist?.Visibility == PlaylistVisibility.PRIVATE)
            ? User != null && User.UserID == playlist.Author
            : true;

        if (visible && playlist != null)
        {
            Id = playlist.Id;
            PlaylistThumbnail = $"https://i.ytimg.com/vi/{playlist.VideoIds.FirstOrDefault()}/hqdefault.jpg";
            PlaylistTitle = playlist.Name;
            PlaylistDescription = playlist.Description;
            AuthorName = playlist.Author;
            AuthorId = DatabaseManager.Users.GetUserFromId(playlist.Author).Result?.LTChannelID ?? "";
            ViewCountText = "LightTube playlist";
            LastUpdatedText = $"Last updated on {playlist.LastUpdated:MMM d, yyyy}";
            Editable = User != null && User.UserID == playlist.Author;
            Items = DatabaseManager.Playlists.GetPlaylistVideos(playlist.Id, Editable);
        }
        else
        {
            PlaylistThumbnail = $"https://i.ytimg.com/vi//hqdefault.jpg";
            PlaylistTitle = "Playlist unavailable";
            PlaylistDescription = "";
            AuthorName = "";
            AuthorId = "";
            ViewCountText = "LightTube playlist";
            LastUpdatedText = "";
            Items = [];
            Editable = false;
        }
    }
}