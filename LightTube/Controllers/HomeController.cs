using System.Diagnostics;
using LightTube.Contexts;
using Microsoft.AspNetCore.Mvc;
using LightTube.Models;

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
}