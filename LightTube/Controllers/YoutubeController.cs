using System.Text.Json;
using InnerTube;
using InnerTube.Models;
using InnerTube.Protobuf.Params;
using InnerTube.Protobuf.Responses;
using LightTube.Contexts;
using LightTube.Database;
using LightTube.Database.Models;
using LightTube.Localization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Endpoint = InnerTube.Protobuf.Endpoint;

namespace LightTube.Controllers;

public class YoutubeController(SimpleInnerTubeClient innerTube, HttpClient client) : Controller
{
	[Route("/embed/{v}")]
	public async Task<IActionResult> Embed(string v, bool contentCheckOk, bool compatibility = false)
	{
		InnerTubePlayer? player;
		Exception? e;
		try
		{
			player = await innerTube.GetVideoPlayerAsync(v, contentCheckOk, HttpContext.GetInnerTubeLanguage(),
				HttpContext.GetInnerTubeRegion());
			e = null;
		}
		catch (Exception ex)
		{
			player = null;
			e = ex;
		}

		SponsorBlockSegment[] sponsors;
		try
		{
			sponsors = await SponsorBlockSegment.GetSponsors(v);
		}
		catch
		{
			sponsors = [];
		}

		if (HttpContext.GetDefaultCompatibility())
			compatibility = true;

		InnerTubeVideo video = await innerTube.GetVideoDetailsAsync(v, contentCheckOk, null, null, null,
			language: HttpContext.GetInnerTubeLanguage(), region: HttpContext.GetInnerTubeRegion());
		if (player is null || e is not null)
			return View(new EmbedContext(HttpContext, e ?? new Exception("player is null"), video));
		return View(new EmbedContext(HttpContext, player, video, compatibility, sponsors));
	}

	[Route("/watch")]
	public async Task<IActionResult> Watch(string v, string? list, bool contentCheckOk, bool compatibility = false)
	{
		InnerTubePlayer? player;
		Exception? e;
		bool localPlaylist = list?.StartsWith("LT-PL") ?? false;
		try
		{
			player = await innerTube.GetVideoPlayerAsync(v, contentCheckOk, HttpContext.GetInnerTubeLanguage(),
				HttpContext.GetInnerTubeRegion());
			e = null;
			if (player.Details.Id != v)
			{
				e = new Exception(
					$"YouTube returned a different video than the requested one ({v} != {player.Details.Id})");
				player = null;
			}
		}
		catch (Exception ex)
		{
			player = null;
			e = ex;
		}

		InnerTubeVideo video = await innerTube.GetVideoDetailsAsync(v, contentCheckOk, localPlaylist ? null : list,
			null, null, language: HttpContext.GetInnerTubeLanguage(), region: HttpContext.GetInnerTubeRegion());
		ContinuationResponse? comments = null;

		if (HttpContext.GetDefaultCompatibility())
			compatibility = true;

		try
		{
			comments = await innerTube.GetVideoCommentsAsync(v, CommentsContext.Types.SortOrder.TopComments);
		}
		catch
		{
			/* comments arent enabled, ignore */
		}

		int dislikes;
		try
		{
			HttpResponseMessage rydResponse =
				await client.GetAsync("https://returnyoutubedislikeapi.com/votes?videoId=" + v);
			Dictionary<string, JsonElement> rydJson =
				JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
					await rydResponse.Content.ReadAsStringAsync())!;
			dislikes = rydJson["dislikes"].GetInt32();
		}
		catch
		{
			dislikes = -1;
		}

		SponsorBlockSegment[] sponsors;
		try
		{
			sponsors = await SponsorBlockSegment.GetSponsors(v);
		}
		catch
		{
			sponsors = [];
		}

		if (player is not null)
			await DatabaseManager.Cache.AddVideo(new DatabaseVideo(player), true);

		if (localPlaylist && list != null)
		{
			DatabasePlaylist? pl = DatabaseManager.Playlists.GetPlaylist(list);
			if (player is null || e is not null)
				return View(new WatchContext(HttpContext, e ?? new Exception("player is null"), video, pl, comments,
					dislikes));
			return View(new WatchContext(HttpContext, player, video, pl, comments, compatibility, dislikes,
				sponsors));
		}

		if (player is null || e is not null)
			return View(new WatchContext(HttpContext, e ?? new Exception("player is null"), video, comments,
				dislikes));
		return View(
			new WatchContext(HttpContext, player, video, comments, compatibility, dislikes, sponsors));
	}

	[Route("/results")]
	public async Task<IActionResult> Search(string search_query, string? filter = null, string? continuation = null)
	{
		if (!string.IsNullOrWhiteSpace(search_query))
			Response.Cookies.Append("lastSearch", search_query);
		if (continuation is null)
		{
			SearchParams searchParams = Request.GetSearchParams();

			InnerTubeSearchResults search =
				await innerTube.SearchAsync(search_query, searchParams, HttpContext.GetInnerTubeLanguage(),
					HttpContext.GetInnerTubeRegion());
			return View(new SearchContext(HttpContext, search_query, searchParams, search));
		}
		else
		{
			SearchContinuationResponse search =
				await innerTube.ContinueSearchAsync(continuation, HttpContext.GetInnerTubeLanguage(),
					HttpContext.GetInnerTubeRegion());
			return View(new SearchContext(HttpContext, search_query, null, search));
		}
	}

	[Route("/c/{vanity}")]
	public async Task<IActionResult> ChannelFromVanity(string vanity)
	{
		ResolveUrlResponse endpoint = await innerTube.ResolveUrl("https://youtube.com/c/" + vanity);
		return Redirect(endpoint.Endpoint.EndpointTypeCase == Endpoint.EndpointTypeOneofCase.BrowseEndpoint
			? $"/channel/{endpoint.Endpoint.BrowseEndpoint.BrowseId}"
			: "/");
	}

	[Route("/@{handle}")]
	public async Task<IActionResult> ChannelFromHandle(string handle)
	{
			ResolveUrlResponse endpoint = await innerTube.ResolveUrl("https://youtube.com/@" + handle);
		return Redirect(endpoint.Endpoint.EndpointTypeCase == Endpoint.EndpointTypeOneofCase.BrowseEndpoint
			? $"/channel/{endpoint.Endpoint.BrowseEndpoint.BrowseId}"
			: "/");
	}

	[Route("/channel/{id}")]
	public async Task<IActionResult> Channel(string id, string? continuation = null) =>
		await Channel(id, ChannelTabs.Featured, continuation);

	[Route("/channel/{id}/subscription")]
	[HttpGet]
	public async Task<IActionResult> Subscription(string id)
	{
		if (id.StartsWith("LT")) return BadRequest("You cannot subscribe to other LightTube users");
		InnerTubeChannel channel =
			await innerTube.GetChannelAsync(id, ChannelTabs.Featured, HttpContext.GetInnerTubeLanguage(),
				HttpContext.GetInnerTubeRegion());
		await DatabaseManager.Cache.AddChannel(new DatabaseChannel(channel), true);
		SubscriptionContext ctx = new(HttpContext, channel);
		if (ctx.User is null)
		{
			return Redirect($"/account/login?redirectUrl=%2Fchannel%2F{id}%2Fsubscription");
		}

		return View(ctx);
	}

	[Route("/channel/{id}/subscription")]
	[HttpPost]
	public async Task<IActionResult> Subscription(string id, SubscriptionType type)
	{
		if (id.StartsWith("LT")) return BadRequest("You cannot subscribe to other LightTube users");
		await DatabaseManager.Users.UpdateSubscription(Request.Cookies["token"] ?? "", id, type);
		InnerTubeChannel channel =
			await innerTube.GetChannelAsync(id, ChannelTabs.Featured, HttpContext.GetInnerTubeLanguage(),
				HttpContext.GetInnerTubeRegion());
		await DatabaseManager.Cache.AddChannel(new DatabaseChannel(channel));
		return Ok(LocalizationManager.GetFromHttpContext(HttpContext).GetRawString("modal.close"));
	}

	[Route("/channel/{id}/about")]
	public async Task<IActionResult> Channel(string id)
	{
		if (id.StartsWith("LT"))
		{
			// nuh uh
			return Redirect($"/channel/{id}");
		}

		InnerTubeChannel channel = await innerTube.GetChannelAsync(id, ChannelTabs.Featured,
			HttpContext.GetInnerTubeLanguage(), HttpContext.GetInnerTubeRegion());
		InnerTubeAboutChannel? about = await innerTube.GetAboutChannelAsync(id, HttpContext.GetInnerTubeLanguage(),
			HttpContext.GetInnerTubeRegion());
		if (about == null)
		{
			return Redirect($"/channel/{id}");
		}

		try
		{
			await DatabaseManager.Cache.AddChannel(new DatabaseChannel(channel), true);
		}
		catch (Exception)
		{
			// ignored
		}

		return View(new ChannelContext(HttpContext, ChannelTabs.About, channel, id, about));
	}

	[Route("/channel/{id}/{tab}")]
	public async Task<IActionResult> Channel(string id, ChannelTabs tab = ChannelTabs.Featured, string? continuation = null)
	{
		if (id.StartsWith("LT"))
		{
			DatabaseUser? user = await DatabaseManager.Users.GetUserFromLTId(id);
			return View(new ChannelContext(HttpContext, user, id));
		}

		if (continuation is null)
		{
			InnerTubeChannel channel = await innerTube.GetChannelAsync(id, tab, HttpContext.GetInnerTubeLanguage(),
				HttpContext.GetInnerTubeRegion());
			try
			{
				await DatabaseManager.Cache.AddChannel(new DatabaseChannel(channel), true);
			}
			catch (Exception)
			{
				// ignored
			}

			return View(new ChannelContext(HttpContext, tab, channel, id));
		}
		else
		{
			InnerTubeChannel channel = await innerTube.GetChannelAsync(id, tab, HttpContext.GetInnerTubeLanguage(),
				HttpContext.GetInnerTubeRegion());
			ContinuationResponse cont = await innerTube.ContinueChannelAsync(continuation,
				HttpContext.GetInnerTubeLanguage(), HttpContext.GetInnerTubeRegion());
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
			InnerTubePlaylist playlist = await innerTube.GetPlaylistAsync(list, true, PlaylistFilter.All,
				HttpContext.GetInnerTubeLanguage(), HttpContext.GetInnerTubeRegion());
			if (continuation is null)
			{
				return View(new PlaylistContext(HttpContext, playlist));
			}
			else
			{
				ContinuationResponse continuationRes = await innerTube.ContinuePlaylistAsync(continuation,
					HttpContext.GetInnerTubeLanguage(), HttpContext.GetInnerTubeRegion());
				return View(new PlaylistContext(HttpContext, playlist, continuationRes));
			}
		}
	}

	[Route("/shorts/{v}")]
	public IActionResult Shorts(string v) => RedirectPermanent($"/watch?v={v}");

	[Route("/download/{v}")]
	public async Task<IActionResult> Download(string v)
	{
		InnerTubePlayer? player;
		Exception? e;

		try
		{
			player = await innerTube.GetVideoPlayerAsync(v, true, HttpContext.GetInnerTubeLanguage(),
				HttpContext.GetInnerTubeRegion());
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
		PlaylistVideoContext<InnerTubePlayer> ctx = new(HttpContext);
		ctx.ItemId = player.Details.Id;
		ctx.ItemTitle = player.Details.Title;
		ctx.ItemSubtitle = player.Details.Author.Title;
		ctx.ItemThumbnail = $"https://i.ytimg.com/vi/{player.Details.Id}/hqdefault.jpg";
		ctx.Extra = player;
		ctx.Title = ctx.Localization.GetRawString("download.title");
		ctx.AlignToStart = true;
		ctx.Buttons = [];
		return View(ctx);
	}
}