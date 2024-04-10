using System.Text;
using LightTube.Contexts;
using LightTube.Database;
using LightTube.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace LightTube.Controllers;

[Route("/export")]
public class ExportController : Controller
{
    [Route("fullData.json")]
    public IActionResult LightTubeExport()
    {
        BaseContext context = new(HttpContext);
        if (context.User == null) return Redirect("/account/login?redirectUrl=%2fexport%2ffullData.json");

        LightTubeExport export = new()
        {
            Type = $"LightTube/{Utils.GetVersion()}",
            Host = Request.Host.ToString(),
            Subscriptions = [.. context.User.Subscriptions.Keys],
            Playlists = DatabaseManager.Playlists.GetUserPlaylists(context.User.UserID, PlaylistVisibility.PRIVATE)
                .Select(x => new ImportedData.Playlist
                {
                    Title = x.Name,
                    Description = x.Description,
                    TimeCreated = null,
                    TimeUpdated = x.LastUpdated,
                    Visibility = x.Visibility,
                    VideoIds = [.. x.VideoIds]
                }).ToArray()
        };
        return File(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(export)), "application/json",
            $"LightTubeExport_{context.User.UserID}.json");
    }
}