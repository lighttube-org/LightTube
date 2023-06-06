using InnerTube;

namespace LightTube.Contexts;

public class PlayerContext : BaseContext
{
	public InnerTubePlayer? Player;
	public Exception? Exception;
	public bool UseHls;
	public bool UseDash;
	public Thumbnail[] Thumbnails = Array.Empty<Thumbnail>();
	public string? ErrorMessage = null;
	public string PreferredItag = "18";
	public bool UseEmbedUi = false;
	public string? ClassName;

	public PlayerContext(HttpContext context, InnerTubePlayer innerTubePlayer, string className, bool compatibility,
		string preferredItag) : base(context)
	{
		Player = innerTubePlayer;
		ClassName = className;
		PreferredItag = preferredItag;
		UseHls = innerTubePlayer.DashManifestUrl is not null && !compatibility; // Prefer HLS if the video is live
		// Live videos contain a DASH manifest URL
		UseDash = innerTubePlayer.AdaptiveFormats.Any() && !compatibility; // Prefer DASH if we can provide Adaptive
		// Formats
		if (Configuration.GetVariable("LIGHTTUBE_DISABLE_PROXY", "false") != "false")
		{
			UseHls = false;
			UseDash = false;
		}
	}

	public PlayerContext(HttpContext context, Exception e) : base(context)
	{
		Exception = e;
	}

	public string? GetFirstItag() => GetPreferredFormat()?.Itag;

	public Format? GetPreferredFormat() =>
		Player?.Formats.FirstOrDefault(x => x.Itag == PreferredItag && x.Itag != "17") ??
		Player?.Formats.FirstOrDefault(x => x.Itag != "17");

	public string GetClass() => ClassName is not null ? $" {ClassName}" : "";

	public IEnumerable<Format> GetFormatsInPreferredOrder()
	{
		return Player!.Formats.OrderBy(x => x.Itag != PreferredItag);
	}
}