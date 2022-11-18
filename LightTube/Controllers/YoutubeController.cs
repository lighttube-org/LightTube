using InnerTube;
using LightTube.Contexts;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers;

public class YoutubeController : Controller
{
	private readonly InnerTube.InnerTube _youtube;

	public YoutubeController(InnerTube.InnerTube youtube)
	{
		_youtube = youtube;
	}

	[Route("/embed/{v}")]
	public async Task<IActionResult> Embed(string v, bool contentCheckOk, bool compatibility = false)
	{
		InnerTubePlayer player = await _youtube.GetPlayerAsync(v, contentCheckOk, false, HttpContext.GetLanguage(),
			HttpContext.GetRegion());
		InnerTubeNextResponse video =
			await _youtube.GetVideoAsync(v, language: HttpContext.GetLanguage(), region: HttpContext.GetRegion());
		return View(new EmbedContext(player, video, compatibility));
	}

	[Route("/watch")]
	public async Task<IActionResult> Watch(string v, string list, bool contentCheckOk, bool compatibility = false)
	{
		InnerTubePlayer player = await _youtube.GetPlayerAsync(v, contentCheckOk, false, HttpContext.GetLanguage(),
			HttpContext.GetRegion());
		InnerTubeNextResponse video =
			await _youtube.GetVideoAsync(v, list, language: HttpContext.GetLanguage(), region: HttpContext.GetRegion());
		return View(new WatchContext(player, video, compatibility, HttpContext));
	}
}