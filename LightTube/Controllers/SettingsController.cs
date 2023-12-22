using System.Diagnostics;
using InnerTube;
using LightTube.Contexts;
using LightTube.Database;
using LightTube.Database.Models;
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
	public IActionResult Appearance(string hl, string gl, string theme, string recommendations, string compatibility)
	{
		Response.Cookies.Append("hl", hl, new CookieOptions
		{
			Expires = DateTimeOffset.MaxValue
		});
		Response.Cookies.Append("gl", gl, new CookieOptions
		{
			Expires = DateTimeOffset.MaxValue
		});
		Response.Cookies.Append("theme", theme, new CookieOptions
		{
			Expires = DateTimeOffset.MaxValue
		});
		Response.Cookies.Append("recommendations", recommendations == "on" ? "visible" : "collapsed", new CookieOptions
		{
			Expires = DateTimeOffset.MaxValue
		});
		Response.Cookies.Append("compatibility", recommendations == "on" ? "true" : "false", new CookieOptions
		{
			Expires = DateTimeOffset.MaxValue
		});
		return Redirect("/settings/appearance");
	}

	[Route("account")]
	public IActionResult Account()
	{
		BaseContext context = new(HttpContext);
		if (context.User == null) return Redirect("/account/login?redirectUrl=%2fsettings%2fdata");

		return View(new BaseContext(HttpContext));
	}

	[Route("data")]
	[HttpGet]
	[HttpPost]
	public IActionResult ImportExport()
	{
		BaseContext context = new(HttpContext);
		if (context.User == null) return Redirect("/account/login?redirectUrl=%2fsettings%2fdata");

		if (Request.Method != "POST") return View(new ImportContext(HttpContext));

		try
		{
			IFormFile file = Request.Form.Files[0];
			using Stream fileStream = file.OpenReadStream();
			using MemoryStream memStr = new();
			fileStream.CopyTo(memStr);
			byte[] bytes = memStr.ToArray();
			ImportedData importedData = ImporterUtility.ExtractData(bytes);
			string[] channelIds = importedData.Subscriptions.Select(x => x.Id).Distinct().ToArray();
			ImportedData.Playlist[] playlists = importedData.Playlists.ToArray();
			string[] videos = importedData.Playlists.SelectMany(x => x.VideoIds).Distinct().ToArray();
			memStr.Dispose();
			fileStream.Dispose();

			string token = Request.Cookies["token"] ?? "";

			// import channels
			Task.Run(() =>
			{
				foreach (string[] ids in channelIds.Chunk(50))
				{
					Stopwatch sp = Stopwatch.StartNew();
					Task[] channelTasks = ids.Select(id => Task.Run(async () =>
						{
							try
							{
								await DatabaseManager.Users.UpdateSubscription(token, id,
									SubscriptionType.NOTIFICATIONS_ON);
							}
							catch (Exception)
							{
								// simply ignore 😇
							}
						}))
						.ToArray();

					try
					{
						Task.WaitAll(channelTasks, TimeSpan.FromSeconds(30));
						sp.Stop();
						Console.WriteLine(
							$"Subscribed to {channelTasks.Length} more channels in {sp.Elapsed}.");
					}
					catch (Exception)
					{
						// simply ignore 😇
					}
				}
			});

			// import playlists
			Task.Run(async () =>
			{
				Dictionary<string, InnerTubePlayer> videoNexts = new();
				foreach (string[] videoIds in videos.Chunk(100))
				{
					Stopwatch sp = Stopwatch.StartNew();
					Task[] videoTasks = videoIds.Select(id => Task.Run(async () =>
						{
							try
							{
								InnerTubePlayer video = await _youtube.GetPlayerAsync(id, true);
								videoNexts.Add(id, video);
							}
							catch (Exception e)
							{
								Console.WriteLine("error/video: " + e.Message);
							}
						}))
						.ToArray();

					try
					{
						Task.WaitAll(videoTasks, TimeSpan.FromSeconds(30));
						sp.Stop();
						Console.WriteLine(
							$"Got {videoTasks.Length} more videos in {sp.Elapsed}. {videoNexts.Count} success");
					}
					catch (Exception e)
					{
						Console.WriteLine("Error while getting videos\n" + e);
					}
				}

				Console.WriteLine(
					$"From {videos.Length} videos, got {videoNexts.Count} videos.");

				foreach (ImportedData.Playlist playlist in playlists)
				{
					DatabasePlaylist pl = await DatabaseManager.Playlists.CreatePlaylist(token, playlist.Title,
						playlist.Description, playlist.Visibility);
					foreach (string video in playlist.VideoIds)
					{
						if (!videoNexts.ContainsKey(video)) continue;
						await DatabaseManager.Playlists.AddVideoToPlaylist(token, pl.Id,
							videoNexts[video]);
					}
				}
			});

			return View(new ImportContext(HttpContext,
				$"Import process started. It might take a few minutes for all the content to appear on your account\n{channelIds.Length} channels, {playlists.Length} playlists, {videos.Length} videos",
				false));
		}
		catch (Exception e)
		{
			return View(new ImportContext(HttpContext, e.Message, true));
		}
	}
}