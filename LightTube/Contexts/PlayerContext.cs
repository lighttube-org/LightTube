using InnerTube;
using InnerTube.Renderers;
using Newtonsoft.Json;

namespace LightTube.Contexts;

public class PlayerContext : BaseContext
{
	public InnerTubePlayer? Player;
	public InnerTubeNextResponse Video;
	public Exception? Exception;
	public bool UseHls;
	public bool UseDash;
	public Thumbnail[] Thumbnails = Array.Empty<Thumbnail>();
	public string? ErrorMessage = null;
	public string PreferredItag = "18";
	public bool UseEmbedUi = false;
	public string? ClassName;
	public SponsorBlockSegment[] Sponsors;

	public PlayerContext(HttpContext context, InnerTubePlayer innerTubePlayer, InnerTubeNextResponse video,
		string className, bool compatibility,
		string preferredItag, SponsorBlockSegment[] sponsors) : base(context)
	{
		Player = innerTubePlayer;
		Video = video;
		ClassName = className;
		PreferredItag = preferredItag;
		Sponsors = sponsors;
		UseHls = !compatibility; // Prefer HLS
		UseDash = innerTubePlayer.AdaptiveFormats.Any() && !compatibility;
		// Formats
		if (Configuration.GetVariable("LIGHTTUBE_DISABLE_PROXY", "false") != "false")
		{
			UseHls = false;
			UseDash = false;
		}
	}

	public string GetChaptersJson()
	{
		if (Video.Chapters is null) return "[]";
		ChapterRenderer[] c = Video.Chapters.ToArray();
		List<LtVideoChapter> ltChapters = new();
		for (int i = 0; i < c.Length; i++)
		{
			ChapterRenderer chapter = c[i];
			float to = 100;
			if (i + 1 < c.Length)
			{
				ChapterRenderer next = c[i + 1];
				to = next.TimeRangeStartMillis / (float)Player!.Details.Length.TotalMilliseconds * 100;
			}
			ltChapters.Add(new LtVideoChapter
			{
				From = chapter.TimeRangeStartMillis / (float)Player!.Details.Length.TotalMilliseconds * 100,
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
		Sponsors = Array.Empty<SponsorBlockSegment>();
	}

	public string? GetFirstItag() => GetPreferredFormat()?.Itag;

	public Format? GetPreferredFormat() =>
		Player?.Formats.FirstOrDefault(x => x.Itag == PreferredItag && x.Itag != "17") ??
		Player?.Formats.FirstOrDefault(x => x.Itag != "17");

	public string GetClass() => ClassName is not null ? $" {ClassName}" : "";

	public IEnumerable<Format> GetFormatsInPreferredOrder() => Player!.Formats.OrderBy(x => x.Itag != PreferredItag).Where(x => x.Itag != "17");
}