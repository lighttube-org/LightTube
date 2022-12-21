using System.Text.Json;
using InnerTube;
using LightTube.Contexts;
using LightTube.Database;
using LightTube.Database.Models;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers;

public class YoutubeController : Controller
{
	private readonly InnerTube.InnerTube _youtube;
	private readonly HttpClient _client;

	public YoutubeController(InnerTube.InnerTube youtube, HttpClient client)
	{
		_youtube = youtube;
		_client = client;
	}

	[Route("/embed/{v}")]
	public async Task<IActionResult> Embed(string v, bool contentCheckOk, bool compatibility = false)
	{
		InnerTubePlayer player;
		Exception? e;
		try
		{
			player = await _youtube.GetPlayerAsync(v, contentCheckOk, false, HttpContext.GetLanguage(),
				HttpContext.GetRegion());
			e = null;
		}
		catch (Exception ex)
		{
			player = null;
			e = ex;
		}

		InnerTubeNextResponse video =
			await _youtube.GetVideoAsync(v, language: HttpContext.GetLanguage(), region: HttpContext.GetRegion());
		if (player is null || e is not null)
			return View(new EmbedContext(HttpContext, e ?? new Exception("player is null"), video));
		return View(new EmbedContext(HttpContext, player, video, compatibility));
	}

	[Route("/watch")]
	public async Task<IActionResult> Watch(string v, string? list, bool contentCheckOk, bool compatibility = false)
	{
		InnerTubePlayer? player;
		Exception? e;
		bool localPlaylist = list?.StartsWith("LT-PL") ?? false;
		try
		{
			player = await _youtube.GetPlayerAsync(v, contentCheckOk, false, HttpContext.GetLanguage(),
				HttpContext.GetRegion());
			e = null;
		}
		catch (Exception ex)
		{
			player = null;
			e = ex;
		}

		InnerTubeNextResponse video =
			await _youtube.GetVideoAsync(v, localPlaylist ? null : list, language: HttpContext.GetLanguage(), region: HttpContext.GetRegion());
		InnerTubeContinuationResponse? comments = null;

		if (video.CommentsContinuation is not null)
			comments = await _youtube.GetVideoCommentsAsync(video.CommentsContinuation,
				language: HttpContext.GetLanguage(),
				region: HttpContext.GetRegion());

		int dislikes;
		try
		{
			HttpResponseMessage rydResponse =
				await _client.GetAsync("https://returnyoutubedislikeapi.com/votes?videoId=" + v);
			Dictionary<string, JsonElement> rydJson =
				JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
					await rydResponse.Content.ReadAsStringAsync())!;
			dislikes = rydJson["dislikes"].GetInt32();
		}
		catch
		{
			dislikes = -1;
		}

		if (player is not null)
			await DatabaseManager.Cache.AddVideo(new DatabaseVideo(player), true);

		if (localPlaylist && list != null)
		{
			DatabasePlaylist? pl = DatabaseManager.Playlists.GetPlaylist(list);
			if (player is null || e is not null)
				return View(new WatchContext(HttpContext, e ?? new Exception("player is null"), video, pl, comments, dislikes));
			return View(new WatchContext(HttpContext, player, video, pl, comments, compatibility, dislikes));
		}
		else
		{
			if (player is null || e is not null)
				return View(new WatchContext(HttpContext, e ?? new Exception("player is null"), video, comments, dislikes));
			return View(new WatchContext(HttpContext, player, video, comments, compatibility, dislikes));
		}
	}

	[Route("/results")]
	public async Task<IActionResult> Search(string search_query, string? filter = null, string? continuation = null)
	{
		if (!string.IsNullOrWhiteSpace(search_query))
			Response.Cookies.Append("lastSearch", search_query);
		if (continuation is null)
		{
			InnerTubeSearchResults search =
				await _youtube.SearchAsync(search_query, filter, HttpContext.GetLanguage(), HttpContext.GetRegion());
			return View(new SearchContext(HttpContext, search_query, filter, search));
		}
		else
		{
			InnerTubeContinuationResponse search =
				await _youtube.ContinueSearchAsync(continuation, HttpContext.GetLanguage(), HttpContext.GetRegion());
			return View(new SearchContext(HttpContext, search_query, filter, search));
		}
	}

	[Route("/c/{vanity}")]
	public async Task<IActionResult> ChannelFromVanity(string vanity)
	{
		string? id = await _youtube.GetChannelIdFromVanity(vanity);
		return Redirect(id is null ? "/" : $"/channel/{id}");
	}

	[Route("/@{vanity}")]
	public async Task<IActionResult> ChannelFromHandle(string vanity)
	{
		string? id = await _youtube.GetChannelIdFromVanity("@" + vanity);
		return Redirect(id is null ? "/" : $"/channel/{id}");
	}

	[Route("/channel/{id}")]
	public async Task<IActionResult> Channel(string id, string? continuation = null) =>
		await Channel(id, ChannelTabs.Home, continuation);

	[Route("/channel/{id}/subscription")]
	[HttpGet]
	public async Task<IActionResult> Subscription(string id)
	{
		if (id.StartsWith("LT")) return BadRequest("You cannot subscribe to other LightTube users");
		InnerTubeChannelResponse channel =
			await _youtube.GetChannelAsync(id, ChannelTabs.Home, null, HttpContext.GetLanguage(),
				HttpContext.GetRegion());
		await DatabaseManager.Cache.AddChannel(new DatabaseChannel(channel), true);
		return View(new SubscriptionContext(HttpContext, channel));
	}

	[Route("/channel/{id}/subscription")]
	[HttpPost]
	public async Task<IActionResult> Subscription(string id, SubscriptionType type)
	{
		if (id.StartsWith("LT")) return BadRequest("You cannot subscribe to other LightTube users");
		(string? _, SubscriptionType subscriptionType) =
			await DatabaseManager.Users.UpdateSubscription(Request.Cookies["token"] ?? "", id, type);
		InnerTubeChannelResponse channel =
			await _youtube.GetChannelAsync(id, ChannelTabs.Home, null, HttpContext.GetLanguage(),
				HttpContext.GetRegion());
		await DatabaseManager.Cache.AddChannel(new DatabaseChannel(channel));
		return Ok("You can now close this window.");
	}

	[Route("/channel/{id}/{tab}")]
	public async Task<IActionResult> Channel(string id, ChannelTabs tab = ChannelTabs.Home, string? continuation = null)
	{
		if (id.StartsWith("LT"))
		{
			DatabaseUser? user = await DatabaseManager.Users.GetUserFromLTId(id);
			return View(new ChannelContext(HttpContext, user, id));
		}

		if (continuation is null)
		{
			InnerTubeChannelResponse channel =
				await _youtube.GetChannelAsync(id, tab, null, HttpContext.GetLanguage(), HttpContext.GetRegion());
			await DatabaseManager.Cache.AddChannel(new DatabaseChannel(channel), true);
			return View(new ChannelContext(HttpContext, tab, channel, id));
		}
		else
		{
			InnerTubeChannelResponse channel =
				await _youtube.GetChannelAsync(id, tab, null, HttpContext.GetLanguage(), HttpContext.GetRegion());
			InnerTubeContinuationResponse cont =
				await _youtube.ContinueChannelAsync(continuation, HttpContext.GetLanguage(), HttpContext.GetRegion());
			return View(new ChannelContext(HttpContext, tab, channel, cont, id));
		}
	}

	[Route("/playlist")]
	public async Task<IActionResult> Playlist(string list, string? continuation = null)
	{
		if (list.StartsWith("LT-PL"))
		{
			DatabasePlaylist? playlist = DatabaseManager.Playlists.GetPlaylist(list);
			return View(new PlaylistContext(HttpContext, playlist));
		}
		else
		{
			InnerTubePlaylist playlist =
				await _youtube.GetPlaylistAsync(list, true, HttpContext.GetLanguage(), HttpContext.GetRegion());
			if (continuation is null)
			{
				return View(new PlaylistContext(HttpContext, playlist));
			}
			else
			{
				InnerTubeContinuationResponse continuationRes =
					await _youtube.ContinuePlaylistAsync(continuation, HttpContext.GetLanguage(), HttpContext.GetRegion());
				return View(new PlaylistContext(HttpContext, playlist, continuationRes));
			}
		}
	}

	[Route("/shorts/{v}")]
	public IActionResult Shorts(string v) => RedirectPermanent("/watch?v={v}");

	[Route("/download/{v}")]
	public async Task<IActionResult> Download(string v)
	{
		InnerTubePlayer? player;
		Exception? e;

		try
		{
			player = await _youtube.GetPlayerAsync(v, true, false, HttpContext.GetLanguage(),
				HttpContext.GetRegion());
			e = null;
		}
		catch (Exception ex)
		{
			player = null;
			e = ex;
		}

		if (player is null || e is not null)
			return BadRequest(e?.Message ?? "player is null");
		if (player.Details.IsLive)
			return BadRequest("You cannot download live videos");
		PlaylistVideoContext<InnerTubePlayer> ctx = new PlaylistVideoContext<InnerTubePlayer>(HttpContext);
		ctx.ItemId = player.Details.Id;
		ctx.ItemTitle = player.Details.Title;
		ctx.ItemSubtitle = player.Details.Author.Title;
		ctx.ItemThumbnail = $"https://i.ytimg.com/vi/{player.Details.Id}/hqdefault.jpg";
		ctx.Extra = player;
		ctx.Title = "Download video";
		ctx.AlignToStart = true;
		ctx.Buttons = Array.Empty<ModalButton>();
		return View(ctx);
	}
}