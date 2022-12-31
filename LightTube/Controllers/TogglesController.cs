using LightTube.Contexts;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers;

[Route("/toggles")]
public class TogglesController : Controller
{
	[Route("theme")]
	public IActionResult ToggleTheme(string url)
	{
		BaseContext bc = new(HttpContext);
		string newTheme = bc.IsDarkMode() ? "light" : "dark";

		HttpContext.Response.Cookies.Append("theme", newTheme, new CookieOptions
		{
			Expires = DateTimeOffset.MaxValue
		});

		return Redirect(url);
	}
}