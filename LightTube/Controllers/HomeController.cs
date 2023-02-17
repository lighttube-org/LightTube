using System.Text;
using LightTube.Contexts;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers;

public class HomeController : Controller
{
	private readonly ILogger<HomeController> _logger;

	public HomeController(ILogger<HomeController> logger)
	{
		_logger = logger;
	}

	public IActionResult Index() => View(new BaseContext(HttpContext));

	[Route("/rss")]
	public IActionResult Rss() => View(new BaseContext(HttpContext));

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error() => View(new BaseContext(HttpContext));

	[Route("/css/custom.css")]
	public IActionResult CustomCss()
	{
		string? fileName = Configuration.GetVariable("LIGHTTUBE_CUSTOM_CSS_PATH");

		if (fileName != null)
		{
			using FileStream fs = System.IO.File.OpenRead(fileName);
			using StreamReader sr = new(fs);
			string contents = sr.ReadToEnd();
			fs.Close();
			return File(Encoding.UTF8.GetBytes(contents), "text/css");
		}

		return NotFound();
	}
}