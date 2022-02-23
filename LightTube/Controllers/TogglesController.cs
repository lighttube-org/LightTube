using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers
{
	[Route("/toggles")]
	public class TogglesController : Controller
	{
		[Route("theme")]
		public IActionResult ToggleTheme(string redirectUrl)
		{
			if (Request.Cookies.TryGetValue("theme", out string theme))
				Response.Cookies.Append("theme", theme switch
				{
					"light" => "dark",
					"dark" => "light",
					var _ => "dark"
				});
			else
				Response.Cookies.Append("theme", "light");

			return Redirect(redirectUrl);
		}

		[Route("compatibility")]
		public IActionResult ToggleCompatibility(string redirectUrl)
		{
			if (Request.Cookies.TryGetValue("compatibility", out string compatibility))
				Response.Cookies.Append("compatibility", compatibility switch
				{
					"true" => "false",
					"false" => "true",
					var _ => "true"
				});
			else
				Response.Cookies.Append("compatibility", "true");

			return Redirect(redirectUrl);
		}
	}
}