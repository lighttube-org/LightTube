using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using InnerTube;
using InnerTube.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Serilog;

namespace LightTube.Controllers;

[Route("/proxy")]
public class ProxyController : Controller
{
	private readonly InnerTube.InnerTube _youtube;
	private readonly HttpClient client = new HttpClient();

	private string[] _blockedHeaders =
	{
		"host",
		"cookie",
		"cookies",
		"accept-encoding",
		"if-none-match",
		"access-control-allow-origin",
		"access-control-allow-credentials",
		"timing-allow-origin",
		"access-control-expose-headers",
		"vary",
		"cross-origin-resource-policy"
	};

	public ProxyController(InnerTube.InnerTube youtube)
	{
		_youtube = youtube;
	}

	[Route("media/{videoId}/{formatId}")]
	public async Task Media(string videoId, string formatId, string? audioTrackId)
	{
		if (Configuration.GetVariable("LIGHTTUBE_DISABLE_PROXY", "false") != "false")
		{
			Response.StatusCode = (int)HttpStatusCode.NotFound;
			await Response.Body.WriteAsync(Encoding.UTF8.GetBytes("This instance has disabled media proxies."));
			await Response.StartAsync();
		}

		try
		{
			InnerTubePlayer player = await _youtube.GetPlayerAsync(videoId, true, false);
			List<Format> formats = new();
			formats.AddRange(player.Formats);
			formats.AddRange(player.AdaptiveFormats);
			if (formats.All(x => x.Itag != formatId))
			{
				Response.StatusCode = (int)HttpStatusCode.NotFound;
				await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(
					$"Format with ID {formatId} not found.\nAvailable IDs are: {string.Join(", ", formats.Select(x => x.Itag.ToString()))}"));
				await Response.StartAsync();
				return;
			}

			Format format = !string.IsNullOrWhiteSpace(audioTrackId)
				? formats.First(x => x.AudioTrack?.Id == audioTrackId)
				: formats.OrderBy(x => !x.AudioTrack?.AudioIsDefault).First(x => x.Itag == formatId);
			string url = format.Url.ToString();

			if (!url.StartsWith("http://") && !url.StartsWith("https://"))
				url = "https://" + url;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			request.Method = Request.Method;

			foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
				         !header.Key.StartsWith(":") && !_blockedHeaders.Contains(header.Key.ToLower())))
			foreach (string value in values)
				request.Headers.Add(header, value);

			HttpWebResponse response;

			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException e)
			{
				response = (HttpWebResponse)e.Response;
			}

			if (response == null)
			{
				await Response.StartAsync();
				return;
			}

			foreach (string header in response.Headers.AllKeys)
				if (!_blockedHeaders.Contains(header))
					Response.Headers[header] = response.Headers.Get(header);
			Response.Headers.Add("Content-Disposition",
				$"attachment; filename=\"{Regex.Replace(player.Details.Title, @"[^\u0000-\u007F]+", "_")}\"");
			Response.StatusCode = (int)response.StatusCode;

			await using Stream stream = response.GetResponseStream();
			try
			{
				await stream.CopyToAsync(Response.Body, HttpContext.RequestAborted);
			}
			catch (Exception)
			{
				// an exception is thrown if the client suddenly stops streaming
			}

			await Response.StartAsync();
		}
		catch (InnerTubeException e)
		{
			Response.StatusCode = (int)HttpStatusCode.BadGateway;
			await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(e.ToString()));
			await Response.StartAsync();
		}
		catch (Exception e)
		{
			Response.StatusCode = (int)HttpStatusCode.InternalServerError;
			await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(e.ToString()));
			await Response.StartAsync();
		}
	}

	[Route("media/{videoId}.m3u8")]
	public async Task<IActionResult> HlsProxy(string videoId, string formatId, bool useProxy = true,
		bool skipCaptions = false)
	{
		if (Configuration.GetVariable("LIGHTTUBE_DISABLE_PROXY", "false") != "false")
			useProxy = false;

		try
		{
			InnerTubePlayer player = await _youtube.GetPlayerAsync(videoId, true, true);

			if (player.HlsManifestUrl == null)
			{
				Response.StatusCode = (int)HttpStatusCode.NotFound;
				return File(
					new MemoryStream(Encoding.UTF8.GetBytes("This video does not have a valid HLS manifest URL")),
					"text/plain");
			}

			string url = player.HlsManifestUrl;

			string manifest = await Utils.GetProxiedHlsManifest(url, useProxy ? $"https://{Request.Host}/proxy" : null,
				skipCaptions);

			return File(new MemoryStream(Encoding.UTF8.GetBytes(manifest)),
				"application/vnd.apple.mpegurl");
		}
		catch (InnerTubeException e)
		{
			return StatusCode((int)HttpStatusCode.BadGateway, e.Message);
		}
		catch (Exception e)
		{
			return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
		}
	}

	[Route("media/{videoId}.mpd")]
	public async Task<IActionResult> DashProxy(string videoId, string formatId, bool useProxy = true,
		bool skipCaptions = false)
	{
		if (Configuration.GetVariable("LIGHTTUBE_DISABLE_PROXY", "false") != "false")
			useProxy = false;

		try
		{
			InnerTubePlayer player = await _youtube.GetPlayerAsync(videoId, true, false);

			string manifest =
				Utils.GetDashManifest(player, useProxy ? $"https://{Request.Host}/proxy" : null, skipCaptions);

			return File(new MemoryStream(Encoding.UTF8.GetBytes(manifest)),
				"application/dash+xml");
		}
		catch (InnerTubeException e)
		{
			return StatusCode((int)HttpStatusCode.BadGateway, e.Message);
		}
		catch (Exception e)
		{
			return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
		}
	}

	[Route("hls/playlist/{path}")]
	public async Task<IActionResult> HlsPlaylistProxy(string path, bool useProxy = true)
	{
		if (Configuration.GetVariable("LIGHTTUBE_DISABLE_PROXY", "false") != "false")
			return NotFound("This instance has disabled media proxies.");

		try
		{
			string url = "https://manifest.googlevideo.com/api/manifest/hls_playlist" +
			             path.Replace("%2f", "/", StringComparison.OrdinalIgnoreCase);

			string manifest = await Utils.GetProxiedHlsManifest(url, useProxy ? $"https://{Request.Host}/proxy" : null);

			return File(new MemoryStream(Encoding.UTF8.GetBytes(manifest)),
				"application/vnd.apple.mpegurl");
		}
		catch (InnerTubeException e)
		{
			return StatusCode((int)HttpStatusCode.BadGateway, e.Message);
		}
		catch (Exception e)
		{
			return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
		}
	}

	[Route("hls/timedtext/{path}")]
	public async Task<IActionResult> HlsSubtitleProxy(string path, bool useProxy = true)
	{
		if (Configuration.GetVariable("LIGHTTUBE_DISABLE_PROXY", "false") != "false")
			return NotFound("This instance has disabled media proxies.");

		try
		{
			string url = "https://manifest.googlevideo.com/api/manifest/hls_timedtext_playlist" +
			             path.Replace("%2f", "/", StringComparison.OrdinalIgnoreCase);

			string manifest = await Utils.GetProxiedHlsManifest(url, useProxy ? $"https://{Request.Host}/proxy" : null);

			return File(new MemoryStream(Encoding.UTF8.GetBytes(manifest)),
				"application/vnd.apple.mpegurl");
		}
		catch (InnerTubeException e)
		{
			return StatusCode((int)HttpStatusCode.BadGateway, e.Message);
		}
		catch (Exception e)
		{
			return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
		}
	}

	[Route("hls/segment/{path}")]
	public async Task HlsSegmentProxy(string path)
	{
		if (Configuration.GetVariable("LIGHTTUBE_DISABLE_PROXY", "false") != "false")
		{
			Response.StatusCode = (int)HttpStatusCode.NotFound;
			await Response.Body.WriteAsync(Encoding.UTF8.GetBytes("This instance has disabled media proxies."));
			await Response.StartAsync();
		}

		try
		{
			string url = "https://" +
			             path.Replace("%2f", "/", StringComparison.OrdinalIgnoreCase);

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			request.Method = Request.Method;

			HttpWebResponse response;

			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException e)
			{
				response = (HttpWebResponse)e.Response;
			}

			if (response == null)
			{
				await Response.StartAsync();
				return;
			}

			Response.StatusCode = (int)response.StatusCode;

			await using Stream stream = response.GetResponseStream();
			try
			{
				await stream.CopyToAsync(Response.Body, HttpContext.RequestAborted);
			}
			catch (Exception)
			{
				// an exception is thrown if the client suddenly stops streaming
			}

			await Response.StartAsync();
		}
		catch (Exception e)
		{
			await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(e.ToString()));
			if (!Response.HasStarted)
			{
				Response.StatusCode = (int)HttpStatusCode.InternalServerError;
				await Response.StartAsync();
			}
		}
	}

	[Route("caption/{videoId}/{vssId}")]
	public async Task<IActionResult> SubtitleProxy(string videoId, string vssId)
	{
		try
		{
			InnerTubePlayer player = await _youtube.GetPlayerAsync(videoId);
			InnerTubePlayer.VideoCaption?
				subtitle = player.Captions.FirstOrDefault(x => x.VssId == vssId);

			if (subtitle is null)
			{
				Response.StatusCode = (int)HttpStatusCode.NotFound;
				return File(
					new MemoryStream(Encoding.UTF8.GetBytes(
						$"There are no available subtitles for '{vssId}'. Available subtitle IDs are: {string.Join(", ", player.Captions.Select(x => $"{x.VssId} \"{x.Label}\""))}")),
					"text/plain");
			}

			string url = subtitle.BaseUrl.ToString();
			url = url.Contains("fmt=") ? url.Replace("fmt=srv3", "fmt=vtt") : url + "&fmt=vtt";

			if (!url.StartsWith("http://") && !url.StartsWith("https://"))
				url = "https://" + url;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
				         !header.Key.StartsWith(":") && !_blockedHeaders.Contains(header.Key.ToLower())))
			foreach (string value in values)
				request.Headers.Add(header, value);

			using HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			await using Stream stream = response.GetResponseStream();
			using StreamReader reader = new(stream);

			return File(new MemoryStream(Encoding.UTF8.GetBytes(await reader.ReadToEndAsync())),
				"text/vtt");
		}
		catch (InnerTubeException e)
		{
			return StatusCode((int)HttpStatusCode.BadGateway, e.ToString());
		}
		catch (Exception e)
		{
			return StatusCode((int)HttpStatusCode.InternalServerError, e.ToString());
		}
	}

	[Route("thumbnail/{videoId}/{index:int}")]
	public async Task ThumbnailProxy(string videoId, int index = 0)
	{
		if (Configuration.GetVariable("LIGHTTUBE_DISABLE_PROXY", "false") != "false")
		{
			Response.StatusCode = (int)HttpStatusCode.NotFound;
			await Response.Body.WriteAsync(Encoding.UTF8.GetBytes("This instance has disabled media proxies."));
			await Response.StartAsync();
		}

		/*
		if (index == -1) index = player.Thumbnails.Length - 1;
		if (index >= player.Thumbnails.Length)
		{
			Response.StatusCode = 404;
			await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(
				$"Cannot find thumbnail #{index} for {videoId}. The maximum quality is {player.Thumbnails.Length - 1}"));
			await Response.StartAsync();
			return;
		}
		*/
		string url = $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg"; //player.Thumbnails.FirstOrDefault()?.Url;

		if (!url.StartsWith("http://") && !url.StartsWith("https://"))
			url = "https://" + url;

		HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
		request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

		foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
			         !header.Key.StartsWith(":") && !_blockedHeaders.Contains(header.Key.ToLower())))
		foreach (string value in values)
			request.Headers.Add(header, value);

		using HttpWebResponse response = (HttpWebResponse)request.GetResponse();

		foreach (string header in response.Headers.AllKeys)
			if (Response.Headers.ContainsKey(header))
				Response.Headers[header] = response.Headers.Get(header);
			else
				Response.Headers.Add(header, response.Headers.Get(header));
		Response.StatusCode = (int)response.StatusCode;

		await using Stream stream = response.GetResponseStream();
		await stream.CopyToAsync(Response.Body);
		await Response.StartAsync();
	}

	[Route("storyboard/{videoId}")]
	public async Task StoryboardProxy(string videoId)
	{
		try
		{
			InnerTubePlayer player = await _youtube.GetPlayerAsync(videoId);
			if (!player.Storyboard.Levels.Any())
			{
				Response.StatusCode = (int)HttpStatusCode.NotFound;
				await Response.Body.WriteAsync(Encoding.UTF8.GetBytes("No usable storyboard found."));
				await Response.StartAsync();
				return;
			}

			string url = player.Storyboard.Levels[0].ToString();

			HttpRequestMessage hrm = new(HttpMethod.Get, url);
			foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
				         !header.Key.StartsWith(":") && !_blockedHeaders.Contains(header.Key.ToLower())))
			foreach (string value in values)
				if (!hrm.Headers.Contains(header))
					hrm.Headers.Add(header, value);

			HttpResponseMessage response = await client.SendAsync(hrm);

			foreach ((string? header, IEnumerable<string>? values) in response.Headers)
				Response.Headers[header] = values.First();
			Response.StatusCode = (int)response.StatusCode;

			await response.Content.CopyToAsync(Response.Body);
			await Response.StartAsync();
		}
		catch (InnerTubeException e)
		{
			Response.StatusCode = (int)HttpStatusCode.BadGateway;
			await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(e.ToString()));
			await Response.StartAsync();
		}
		catch (Exception e)
		{
			if (!Response.HasStarted)
				Response.StatusCode = (int)HttpStatusCode.InternalServerError;
			await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(e.ToString()));
			await Response.StartAsync();
		}
	}

	[Route("storyboard/{videoId}.vtt")]
	public async Task<IActionResult> VttThumbnailProxy(string videoId)
	{
		try
		{
			InnerTubePlayer player = await _youtube.GetPlayerAsync(videoId);
			if (!player.Storyboard.Levels.Any())
			{
				Response.StatusCode = (int)HttpStatusCode.NotFound;
				return File(new MemoryStream(Encoding.UTF8.GetBytes("No usable storyboard found.")), "text/plain");
			}

			string url = player.Storyboard.Levels[0].ToString();
			TimeSpan duration = player.Details.Length;
			StringBuilder manifest = new();
			double timeBetween = duration.TotalMilliseconds / 100;

			manifest.AppendLine("WEBVTT")
				.AppendLine();

			for (double ms = 0, i = 0; ms < duration.TotalMilliseconds; ms += timeBetween, i++)
			{
				TimeSpan start = TimeSpan.FromMilliseconds(ms);
				TimeSpan end = TimeSpan.FromMilliseconds(ms + timeBetween);
				manifest
					.AppendLine($"{start:hh\\:mm\\:ss\\.fff} --> {end:hh\\:mm\\:ss\\.fff}")
					.AppendLine(
						$"{Request.Scheme}://{Request.Host}/proxy/storyboard/{videoId}#xywh={i % 10 * 48},{Math.Floor(i / 10) * 27},48,27")
					.AppendLine();
			}

			return File(new MemoryStream(Encoding.UTF8.GetBytes(manifest.ToString())), "text/vtt");
		}
		catch (InnerTubeException e)
		{
			Response.StatusCode = (int)HttpStatusCode.BadGateway;
			return File(new MemoryStream(Encoding.UTF8.GetBytes(e.ToString())), "text/plain");
		}
		catch (Exception e)
		{
			if (!Response.HasStarted)
				Response.StatusCode = (int)HttpStatusCode.InternalServerError;
			return File(new MemoryStream(Encoding.UTF8.GetBytes(e.ToString())), "text/plain");
		}
	}
}