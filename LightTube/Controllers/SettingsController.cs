using InnerTube;
using LightTube.Contexts;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers;

[Route("/settings")]
public class SettingsController : Controller
{
	private readonly InnerTube.InnerTube _youtube;

	public SettingsController(InnerTube.InnerTube youtube)
	{
		_youtube = youtube;
	}

	[Route("content")]
	[HttpGet]
	public async Task<IActionResult> Content() {
		InnerTubeLocals locals = await _youtube.GetLocalsAsync();
		ContentSettingsContext ctx = new ContentSettingsContext(HttpContext, locals);
		return View(ctx);
	}

	[Route("content")]
	[HttpPost]
	public IActionResult Content(string hl, string gl) {
		Response.Cookies.Append("hl", hl);
		Response.Cookies.Append("gl", gl);
		return Redirect("/settings/content");
	}

	[Route("account")]
	public IActionResult Account() => View(new BaseContext(HttpContext));
}