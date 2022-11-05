using InnerTube;

namespace LightTube.Contexts;

public class PlayerContext : BaseContext
{
	public InnerTubePlayer? Player;
	public bool UseHls = true;
	public bool UseDash = true;
	public Thumbnail[] Thumbnails = Array.Empty<Thumbnail>();
	public string? ErrorMessage = null;
	public string PreferredItag = "18";
	public bool UseEmbedUi = false;
	public string? ClassName = "";

	public PlayerContext(InnerTubePlayer innerTubePlayer, string className, bool compatibility)
	{
		Player = innerTubePlayer;
		ClassName = className;
		UseHls = innerTubePlayer.HlsManifestUrl is not null;
		UseDash = innerTubePlayer.DashManifestUrl is not null || innerTubePlayer.AdaptiveFormats.Any();
		if (compatibility)
		{
			UseDash = false;
			UseHls = false;
		}
	}

	public string? GetFirstItag() => Player?.Formats.First(x => x.Itag == PreferredItag && x.Itag != "17").Itag;

	public string GetClass() => ClassName is not null ? $" {ClassName}" : "";
}