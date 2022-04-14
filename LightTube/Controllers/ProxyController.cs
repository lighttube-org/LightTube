using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using InnerTube;
using InnerTube.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace LightTube.Controllers
{
	[Route("/proxy")]
	public class ProxyController : Controller
	{
		private readonly ILogger<YoutubeController> _logger;
		private readonly Youtube _youtube;
		private string[] BlockedHeaders =
		{
			"host"
		};

		public ProxyController(ILogger<YoutubeController> logger, Youtube youtube)
		{
			_logger = logger;
			_youtube = youtube;
		}

		[Route("video")]
		[Obsolete("Use /media instead. Will be removed when the new player is 100% done.")]
		public async Task Proxy(string url)
		{
			if (!url.StartsWith("http://") && !url.StartsWith("https://"))
				url = "https://" + url;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			request.Method = Request.Method;

			foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
				!header.Key.StartsWith(":") && !BlockedHeaders.Contains(header.Key.ToLower())))
				foreach (string value in values)
					request.Headers.Add(header, value);

			HttpWebResponse response;

			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException e)
			{
				response = e.Response as HttpWebResponse;
			}
			
			if (response == null) 
				await Response.StartAsync();

			foreach (string header in response.Headers.AllKeys)
				if (Response.Headers.ContainsKey(header))
					Response.Headers[header] = response.Headers.Get(header);
				else
					Response.Headers.Add(header, response.Headers.Get(header));
			Response.StatusCode = (int)response.StatusCode;

			await using Stream stream = response.GetResponseStream();
			try
			{
				await stream.CopyToAsync(Response.Body, HttpContext.RequestAborted);
			}
			catch (Exception)
			{
			}

			await Response.StartAsync();
		}

		[Route("media/{videoId}/{formatId}")]
		public async Task Media(string videoId, string formatId)
		{
			try
			{
				YoutubePlayer player = await _youtube.GetPlayerAsync(videoId);
				if (!string.IsNullOrWhiteSpace(player.ErrorMessage))
				{
					Response.StatusCode = (int) HttpStatusCode.InternalServerError;
					await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(player.ErrorMessage));
					await Response.StartAsync();
					return;
				}

				List<Format> formats = new();

				formats.AddRange(player.Formats);
				formats.AddRange(player.AdaptiveFormats);

				if (!formats.Any(x => x.FormatId == formatId))
				{
					Response.StatusCode = (int) HttpStatusCode.NotFound;
					await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(
						$"Format with ID {formatId} not found.\nAvailable IDs are: {string.Join(", ", formats.Select(x => x.FormatId.ToString()))}"));
					await Response.StartAsync();
					return;
				}

				string url = formats.First(x => x.FormatId == formatId).Url;

				if (!url.StartsWith("http://") && !url.StartsWith("https://"))
					url = "https://" + url;

				HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
				request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
				request.Method = Request.Method;

				foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
					!header.Key.StartsWith(":") && !BlockedHeaders.Contains(header.Key.ToLower())))
				foreach (string value in values)
					request.Headers.Add(header, value);

				HttpWebResponse response;

				try
				{
					response = (HttpWebResponse) request.GetResponse();
				}
				catch (WebException e)
				{
					response = e.Response as HttpWebResponse;
				}

				if (response == null)
					await Response.StartAsync();

				foreach (string header in response.Headers.AllKeys)
					if (Response.Headers.ContainsKey(header))
						Response.Headers[header] = response.Headers.Get(header);
					else
						Response.Headers.Add(header, response.Headers.Get(header));
				Response.StatusCode = (int) response.StatusCode;

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
				Response.StatusCode = (int) HttpStatusCode.InternalServerError;
				await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(e.ToString()));
				await Response.StartAsync();
			}
		}

		[Route("subtitle")]
		public async Task<FileStreamResult> SubtitleProxy(string url)
		{
			if (!url.StartsWith("http://") && !url.StartsWith("https://"))
				url = "https://" + url;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
				!header.Key.StartsWith(":") && !BlockedHeaders.Contains(header.Key.ToLower())))
				foreach (string value in values)
					request.Headers.Add(header, value);

			using HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			await using Stream stream = response.GetResponseStream();
			using StreamReader reader = new(stream);

			return File(new MemoryStream(Encoding.UTF8.GetBytes(await reader.ReadToEndAsync())),
				"text/vtt");
		}

		[Route("caption/{videoId}/{language}")]
		public async Task<FileStreamResult> SubtitleProxy(string videoId, string language)
		{
			YoutubePlayer player = await _youtube.GetPlayerAsync(videoId);
			if (!string.IsNullOrWhiteSpace(player.ErrorMessage))
			{
				Response.StatusCode = (int) HttpStatusCode.InternalServerError;
				return File(new MemoryStream(Encoding.UTF8.GetBytes(player.ErrorMessage)),
					"text/plain");
			}

			if (!player.Subtitles.Any(x => x.Ext == "vtt" && string.Equals(x.Language, language, StringComparison.InvariantCultureIgnoreCase)))
			{
				Response.StatusCode = (int) HttpStatusCode.NotFound;
				return File(
					new MemoryStream(Encoding.UTF8.GetBytes(
						$"There are no available subtitles for {language}. Available language codes are: {string.Join(", ", player.Subtitles.Select(x => $"\"{x.Language}\""))}")),
					"text/plain");
			}
			string url = player.Subtitles.First(x => x.Ext == "vtt" && string.Equals(x.Language, language, StringComparison.InvariantCultureIgnoreCase)).Url;
			
			if (!url.StartsWith("http://") && !url.StartsWith("https://"))
				url = "https://" + url;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
				!header.Key.StartsWith(":") && !BlockedHeaders.Contains(header.Key.ToLower())))
				foreach (string value in values)
					request.Headers.Add(header, value);

			using HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			await using Stream stream = response.GetResponseStream();
			using StreamReader reader = new(stream);

			return File(new MemoryStream(Encoding.UTF8.GetBytes(await reader.ReadToEndAsync())),
				"text/vtt");
		}

		[Route("image")]
		public async Task ImageProxy(string url)
		{
			if (!url.StartsWith("http://") && !url.StartsWith("https://"))
				url = "https://" + url;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
				!header.Key.StartsWith(":") && !BlockedHeaders.Contains(header.Key.ToLower())))
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
				YoutubePlayer player = await _youtube.GetPlayerAsync(videoId);
				if (!string.IsNullOrWhiteSpace(player.ErrorMessage))
				{
					Response.StatusCode = (int) HttpStatusCode.InternalServerError;
					await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(player.ErrorMessage));
					await Response.StartAsync();
					return;
				}

				if (player.Storyboards.All(x => x.FormatId != "sb2"))
				{
					Response.StatusCode = (int) HttpStatusCode.NotFound;
					await Response.Body.WriteAsync(Encoding.UTF8.GetBytes("No usable storyboard found."));
					await Response.StartAsync();
					return;
				}

				string url = player.Storyboards.First(x => x.FormatId == "sb2").Url;

				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
				request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

				foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
					!header.Key.StartsWith(":") && !BlockedHeaders.Contains(header.Key.ToLower())))
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
			catch (Exception e)
			{
				Response.StatusCode = (int) HttpStatusCode.InternalServerError;
				await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(e.ToString()));
				await Response.StartAsync();
			}
		}

		[Route("hls")]
		public async Task<IActionResult> HlsProxy(string url)
		{
			if (!url.StartsWith("http://") && !url.StartsWith("https://"))
				url = "https://" + url;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
				!header.Key.StartsWith(":") && !BlockedHeaders.Contains(header.Key.ToLower())))
				foreach (string value in values)
					request.Headers.Add(header, value);

			using HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			await using Stream stream = response.GetResponseStream();
			using StreamReader reader = new(stream);
			string manifest = await reader.ReadToEndAsync();
			StringBuilder proxyManifest = new ();
			
			foreach (string s in manifest.Split("\n"))
			{
				// also check if proxy enabled
				proxyManifest.AppendLine(!s.StartsWith("http")
					? s
					: $"https://{Request.Host}/proxy/video?url={HttpUtility.UrlEncode(s)}");
			}

			return File(new MemoryStream(Encoding.UTF8.GetBytes(proxyManifest.ToString())),
				"application/vnd.apple.mpegurl");
		}

		[Route("manifest/{videoId}/{formatId}")]
		public async Task<IActionResult> ManifestProxy(string videoId, string formatId)
		{
			YoutubePlayer player = await _youtube.GetPlayerAsync(videoId);
			if (!string.IsNullOrWhiteSpace(player.ErrorMessage))
			{
				Response.StatusCode = (int) HttpStatusCode.InternalServerError;
				return File(new MemoryStream(Encoding.UTF8.GetBytes(player.ErrorMessage)),
					"text/plain");
			}

			if (player.AdaptiveFormats.All(x => x.FormatId != formatId))
			{
				Response.StatusCode = (int) HttpStatusCode.NotFound;
				return File(
					new MemoryStream(Encoding.UTF8.GetBytes(
						$"Format with ID {formatId} not found.\nAvailable IDs are: {string.Join(", ", player.AdaptiveFormats.Select(x => x.FormatId.ToString()))}")),
					"text/plain");
			}

			string url = player.AdaptiveFormats.First(x => x.FormatId == formatId).Url;
			
			if (!url.StartsWith("http://") && !url.StartsWith("https://"))
				url = "https://" + url;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
				!header.Key.StartsWith(":") && !BlockedHeaders.Contains(header.Key.ToLower())))
				foreach (string value in values)
					request.Headers.Add(header, value);

			using HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			await using Stream stream = response.GetResponseStream();
			using StreamReader reader = new(stream);
			string manifest = await reader.ReadToEndAsync();
			StringBuilder proxyManifest = new ();
			
			foreach (string s in manifest.Split("\n"))
			{
				// also check if proxy enabled
				proxyManifest.AppendLine(!s.StartsWith("http")
					? s
					: $"https://{Request.Host}/proxy/video?url={HttpUtility.UrlEncode(s)}");
			}

			return File(new MemoryStream(Encoding.UTF8.GetBytes(proxyManifest.ToString())),
				"application/vnd.apple.mpegurl");
		}
	}
}