using System.Text;
using LightTube.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace LightTube.Controllers;

public class HomeController(ILogger<HomeController> logger) : Controller
{
    private readonly ILogger<HomeController> _logger = logger;

    public IActionResult Index() => View(new HomepageContext(HttpContext));

    [Route("/rss")]
    public IActionResult Rss() => View(new BaseContext(HttpContext));

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new BaseContext(HttpContext));

    [Route("/css/custom.css")]
    public IActionResult CustomCss()
    {
        string? fileName = Configuration.CustomCssPath;

        if (fileName == null) return NotFound();

        using FileStream fs = System.IO.File.OpenRead(fileName);
        return File(fs, "text/css");
    }

    [Route("/lib/{name}")]
    public IActionResult CachedJs(string name)
    {
        try
        {
            return File(Encoding.UTF8.GetBytes(JsCache.GetJsFileContents(name)),
                name.EndsWith(".css") ? "text/css" : "text/javascript",
                JsCache.CacheUpdateTime, new EntityTagHeaderValue($"\"{JsCache.GetHash(name)}\""));
        }
        catch (Exception e)
        {
            return NotFound();
        }
    }

    [Route("/dismiss_alert")]
    public IActionResult DismissAlert(string redirectUrl)
    {
        if (Configuration.AlertHash == null) return Redirect(redirectUrl);
        Response.Cookies.Append("dismissedAlert", Configuration.AlertHash!, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(15)
        });
        return Redirect(redirectUrl);
    }
}