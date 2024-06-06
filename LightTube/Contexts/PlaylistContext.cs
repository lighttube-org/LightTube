using InnerTube.Models;
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
    public IEnumerable<RendererContainer> Items;
    public string? Continuation;
    public string[] Alerts;

    public PlaylistContext(HttpContext context, InnerTubePlaylist playlist) : base(context)
    {
        Id = playlist.Id;
        PlaylistThumbnail = playlist.Sidebar.Thumbnails.Last().Url;
        PlaylistTitle = playlist.Sidebar.Title;
        PlaylistDescription = playlist.Sidebar.Description;
        AuthorName = playlist.Sidebar.Channel?.Title ?? "????";
        AuthorId = playlist.Sidebar.Channel?.Id ?? "UC";
        ViewCountText = playlist.Sidebar.ViewCountText;
        LastUpdatedText = playlist.Sidebar.LastUpdatedText;
        Editable = false;
        Items = playlist.Contents;
        Continuation = playlist.Continuation;
        Alerts = playlist.Alerts;

        AddMeta("description", playlist.Sidebar.Description);
        AddMeta("author", playlist.Sidebar.Title);
        AddMeta("og:title", playlist.Sidebar.Title);
        AddMeta("og:description", playlist.Sidebar.Description);
        AddMeta("og:url",
            $"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
        AddMeta("og:image",
            $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{(playlist.Contents.First(x => x.Type == "video").Data as VideoRendererData)?.VideoId}/-1");
        AddMeta("twitter:card",
            $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{(playlist.Contents.First(x => x.Type == "video").Data as VideoRendererData)?.VideoId}/-1");
    }

    public PlaylistContext(HttpContext context, InnerTubePlaylist playlist, ContinuationResponse continuation)
        : base(context)
    {
        Id = playlist.Id;
        PlaylistThumbnail = playlist.Sidebar.Thumbnails.Last().Url;
        PlaylistTitle = playlist.Sidebar.Title;
        PlaylistDescription = playlist.Sidebar.Description;
        AuthorName = playlist.Sidebar.Channel?.Title ?? "????";
        AuthorId = playlist.Sidebar.Channel?.Id ?? "UC";
        ViewCountText = playlist.Sidebar.ViewCountText;
        LastUpdatedText = playlist.Sidebar.LastUpdatedText;
        Editable = false;
        Items = continuation.Results;
        Continuation = continuation.ContinuationToken;
        Alerts = [];

        AddMeta("description", playlist.Sidebar.Description);
        AddMeta("author", playlist.Sidebar.Title);
        AddMeta("og:title", playlist.Sidebar.Title);
        AddMeta("og:description", playlist.Sidebar.Description);
        AddMeta("og:url",
            $"{context.Request.Scheme}://{context.Request.Host}/{context.Request.Path}{context.Request.QueryString}");
        AddMeta("og:image",
            $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{(playlist.Contents.First(x => x.Type == "video").Data as VideoRendererData)?.VideoId}/-1");
        AddMeta("twitter:card",
            $"{context.Request.Scheme}://{context.Request.Host}/proxy/thumbnail/{(playlist.Contents.First(x => x.Type == "video").Data as VideoRendererData)?.VideoId}/-1");
    }

    public PlaylistContext(HttpContext context, DatabasePlaylist? playlist) : base(context)
    {
        bool visible = playlist?.Visibility != PlaylistVisibility.Private || User != null && User.UserID == playlist.Author;

        if (visible && playlist != null)
        {
            Id = playlist.Id;
            PlaylistThumbnail = $"https://i.ytimg.com/vi/{playlist.VideoIds.FirstOrDefault()}/hqdefault.jpg";
            PlaylistTitle = playlist.Name;
            PlaylistDescription = playlist.Description;
            AuthorName = playlist.Author;
            AuthorId = DatabaseManager.Users.GetUserFromId(playlist.Author).Result?.LTChannelID ?? "";
            ViewCountText = Localization.GetRawString("playlist.lighttube.views");
            LastUpdatedText = string.Format(Localization.GetRawString("playlist.lastupdated"), playlist.LastUpdated.ToString("MMM d, yyyy"));
            Editable = User != null && User.UserID == playlist.Author;
            Items = DatabaseManager.Playlists.GetPlaylistVideoRenderers(playlist.Id, Editable, Localization);
        }
        else
        {
            Id = "";
            PlaylistThumbnail = "https://i.ytimg.com/vi//hqdefault.jpg";
            PlaylistTitle = Localization.GetRawString("playlist.unavailable");
            PlaylistDescription = "";
            AuthorName = "";
            AuthorId = "";
            ViewCountText = "";
            LastUpdatedText = "";
            Items = [];
            Editable = false;
        }

        Alerts = [];
    }
}