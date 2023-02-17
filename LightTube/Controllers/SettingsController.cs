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

	[Route("/settings")]
	public IActionResult Settings() => RedirectPermanent("/settings/appearance");

	[Route("content")]
	public async Task<IActionResult> Content() => RedirectPermanent("/settings/appearance");

	[Route("appearance")]
	[HttpGet]
	public async Task<IActionResult> Appearance()
	{
		InnerTubeLocals locals = await _youtube.GetLocalsAsync();
		AppearanceSettingsContext ctx = new(HttpContext, locals, Configuration.GetCustomThemeDefs());
		return View(ctx);
	}

	[Route("appearance")]
	[HttpPost]
	public IActionResult Appearance(string hl, string gl, string theme) {
		Response.Cookies.Append("hl", hl, new CookieOptions{
			Expires = DateTimeOffset.MaxValue
		});
		Response.Cookies.Append("gl", gl, new CookieOptions{
			Expires = DateTimeOffset.MaxValue
		});
		Response.Cookies.Append("theme", theme, new CookieOptions{
			Expires = DateTimeOffset.MaxValue
		});
		return Redirect("/settings/appearance");
	}

	[Route("account")]
	public IActionResult Account() => View(new BaseContext(HttpContext));
}