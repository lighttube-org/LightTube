using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace LightTube.Controllers
{
	[Route("/proxy")]
	public class ProxyController : Controller
	{
		private string[] BlockedHeaders =
		{
			"host"
		};

		[Route("video")]
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
	}
}