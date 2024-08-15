using System.Text;
using InnerTube;
using InnerTube.Protobuf.Responses;
using LightTube.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Endpoint = InnerTube.Protobuf.Endpoint;

namespace LightTube.Controllers;

public class HomeController(SimpleInnerTubeClient innerTube) : Controller
{
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

        return File(System.IO.File.ReadAllBytes(fileName), "text/css");
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

    [Route("/{str}")]
    public async Task<IActionResult> AutoRedirect(string str)
    {
        if (str.StartsWith('@'))
        {
            ResolveUrlResponse endpoint = await innerTube.ResolveUrl("https://youtube.com/" + str);
            return Redirect(endpoint.Endpoint.EndpointTypeCase == Endpoint.EndpointTypeOneofCase.BrowseEndpoint
                ? $"/channel/{endpoint.Endpoint.BrowseEndpoint.BrowseId}"
                : "/");
        }

        if (str.Length == 11)
            return Redirect($"/watch?v={str}");

        return Redirect("/");
    }
}