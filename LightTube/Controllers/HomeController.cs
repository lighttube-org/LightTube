using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LightTube.Contexts;
using LightTube.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using InnerTube;
using InnerTube.Models;
using ErrorContext = LightTube.Contexts.ErrorContext;

namespace LightTube.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly Youtube _youtube;

		public HomeController(ILogger<HomeController> logger, Youtube youtube)
		{
			_logger = logger;
			_youtube = youtube;
		}

		public IActionResult Index()
		{
			return View(new BaseContext
			{
				MobileLayout = Utils.IsClientMobile(Request)
			});
		}

		[Route("/compatibility_info")]
		public IActionResult CompatInfo()
		{
			return View(new BaseContext
			{
				MobileLayout = Utils.IsClientMobile(Request)
			});
		}

		[Route("/manifest/{v}.mpd")]
		public async Task<IActionResult> DashManifest(string v)
		{
			YoutubePlayer player = await _youtube.GetPlayerAsync(v, HttpContext.GetLanguage(), HttpContext.GetRegion());
			string manifest = player.GetMpdManifest($"https://{Request.Host}/proxy/video?url=");
			return File(Encoding.UTF8.GetBytes(manifest), "application/dash+xml");
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorContext
			{
				Path = HttpContext.Features.Get<IExceptionHandlerPathFeature>().Path,
				MobileLayout = Utils.IsClientMobile(Request)
			});
		}
	}
}