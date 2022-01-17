using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LightTube.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using YTProxy;

namespace LightTube.Controllers
{
	public class HomeController : Controller
	{
		private string[] BlockedHeaders =
		{
			"host"
		};

		private readonly ILogger<HomeController> _logger;
		private readonly Youtube _youtube;

		public HomeController(ILogger<HomeController> logger, Youtube youtube)
		{
			_logger = logger;
			_youtube = youtube;
		}

		public async Task<IActionResult> Index()
		{
			return View(await _youtube.GetAllEndpoints());
		}

		[Route("/proxy")]
		public async Task Proxy(string url)
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
			Response.OnCompleted(() =>
			{
				Console.WriteLine("Request " + url + " closed");
				stream.Close();
				return Task.CompletedTask;
			});
			await Response.StartAsync();
			/*
			StringBuilder sb = new("curl --insecure -s \\\n");
			int headerRandom = new Random().Next(0, int.MaxValue);

			sb.AppendLine($" -L '{url}' -D /tmp/ytProxy-headers-{headerRandom} \\");
			foreach ((string header, StringValues values) in HttpContext.Request.Headers.Where(header =>
				!header.Key.StartsWith(":") && !BlockedHeaders.Contains(header.Key.ToLower())))
				foreach (string value in values)
					sb.AppendLine($" -H '{header}: {value}' \\");

			sb.AppendLine($" -H 'Host: {url.Split("/")[2]}' \\");
			await System.IO.File.WriteAllTextAsync("/tmp/ytProxy-curlscript-" + headerRandom, sb.ToString());

			ProcessStartInfo psi = new("bash")
			{
				Arguments = "/tmp/ytProxy-curlscript-" + headerRandom,
				RedirectStandardInput = true,
				RedirectStandardOutput = true
			};

			Process proc = Process.Start(psi);
			if (proc == null) throw new Exception();

			while (!System.IO.File.Exists("/tmp/ytProxy-headers-" + headerRandom)) await Task.Delay(1000);

			string[] headersFile = await System.IO.File.ReadAllLinesAsync("/tmp/ytProxy-headers-" + headerRandom);
			foreach (string header in headersFile.Skip(1).Where(x => !x.StartsWith("HTTP")))
			{
				string[] kvp = header.Split(": ");

				if (kvp.Length > 0 && !string.IsNullOrWhiteSpace(kvp[0]) && kvp[0].ToLower() != "content-length")
					if (Response.Headers.ContainsKey(kvp[0]))
						Response.Headers[kvp[0]] = kvp[1];
					else
						Response.Headers.Add(kvp[0], string.Join(":", kvp[1..]));
			}

			Response.Headers.Remove("Location");
			Response.Headers.Add("X-Source-URL", url);
			try
			{
				Response.StatusCode = int.Parse(headersFile.Last(x => x.StartsWith("HTTP/")).Split(" ")[1]);
			}
			catch
			{
				Console.WriteLine(string.Join("\n", headersFile));
			}

			await proc.StandardOutput.BaseStream.CopyToAsync(Response.Body);
			System.IO.File.Delete("/tmp/ytProxy-headers-" + headerRandom);
			System.IO.File.Delete("/tmp/ytProxy-curlscript-" + headerRandom);
			
			Response.OnCompleted(() =>
			{
				proc.Close();
				Console.WriteLine("Closed curl task because client disconnected");
				return Task.CompletedTask;
			});
			await Response.StartAsync();
			*/
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}