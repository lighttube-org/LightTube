using InnerTube.Models;
using InnerTube.Protobuf;
using InnerTube.Protobuf.Responses;
using Newtonsoft.Json;

namespace LightTube.Contexts;

public class PlayerContext : BaseContext
{
    public InnerTubePlayer? Player;
    public InnerTubeVideo? Video;
    public Exception? Exception;
    public bool UseHls;
    public bool UseDash;
    public Thumbnail[] Thumbnails;
    public string? ErrorMessage = null;
    public int PreferredItag = 18;
    public bool UseEmbedUi = false;
    public string? ClassName;
    public SponsorBlockSegment[] Sponsors;
    public bool AudioOnly;

    public PlayerContext(HttpContext context, InnerTubePlayer innerTubePlayer, InnerTubeVideo? video, string className,
        bool compatibility, string? preferredItag, SponsorBlockSegment[] sponsors, bool audioOnly) : base(context)
    {
        Player = innerTubePlayer;
        Video = video;
        ClassName = className;
        PreferredItag = int.TryParse(preferredItag ?? "18", out int itag) ? itag : 18;
        Sponsors = sponsors;
        UseHls = !compatibility && !string.IsNullOrWhiteSpace(innerTubePlayer.HlsManifestUrl); // Prefer HLS
        UseDash = innerTubePlayer.AdaptiveFormats.Any() && !compatibility;
        AudioOnly = audioOnly;
        Thumbnails = innerTubePlayer.Details.Thumbnails;
        // Formats
        if (!Configuration.ProxyEnabled)
        {
            UseHls = false;
            UseDash = false;
        }
    }

    public string GetChaptersJson()
    {
        if (Video?.Chapters is null) return "[]";
        VideoChapter[] c = Video.Chapters.ToArray();
        List<LtVideoChapter> ltChapters = [];
        for (int i = 0; i < c.Length; i++)
        {
            VideoChapter chapter = c[i];
            float to = 100;
            if (i + 1 < c.Length)
            {
                VideoChapter next = c[i + 1];
                to = (next.StartSeconds * 1000) / (float)Player!.Details.Length!.Value.TotalMilliseconds * 100;
            }
            ltChapters.Add(new LtVideoChapter
            {
                From = (chapter.StartSeconds * 1000) / (float)Player!.Details.Length!.Value.TotalMilliseconds * 100,
                To = to,
                Name = chapter.Title
            });
        }

        return JsonConvert.SerializeObject(ltChapters);
    }

    private class LtVideoChapter
    {
        [JsonProperty("from")] public float From;
        [JsonProperty("to")] public float To;
        [JsonProperty("name")] public string Name;
    }

    public PlayerContext(HttpContext context, Exception e) : base(context)
    {
        Exception = e;
        Video = null!;
        Sponsors = [];
    }

    public int? GetFirstItag() => GetPreferredFormat()?.Itag;

    public Format? GetPreferredFormat() =>
        AudioOnly
            ? Player?.AdaptiveFormats.FirstOrDefault(x => x.Mime.StartsWith("audio/"))
            : Player?.Formats.FirstOrDefault();

    public string GetClass() => ClassName is not null ? $" {ClassName}" : "";
}