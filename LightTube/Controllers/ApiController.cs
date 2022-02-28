using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using InnerTube;
using InnerTube.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Chapter = InnerTube.Models.Chapter;
using Format = InnerTube.Models.Format;
using Subtitle = InnerTube.Models.Subtitle;
using Thumbnail = InnerTube.Models.Thumbnail;

namespace LightTube.Controllers
{
	[Route("/api")]
	public class ApiController : Controller
	{
		private const string VideoIdRegex = @"[a-zA-Z0-9_-]{11}";
		private readonly ILogger<ApiController> _logger;
		private readonly Youtube _youtube;

		public ApiController(ILogger<ApiController> logger, Youtube youtube)
		{
			_logger = logger;
			_youtube = youtube;
		}

		private IActionResult Xml(XmlNode xmlDocument)
		{
			MemoryStream ms = new();
			ms.Write(Encoding.UTF8.GetBytes(xmlDocument.OuterXml));
			ms.Position = 0;
			return File(ms, "application/xml");
		}

		[Route("player")]
		public async Task<IActionResult> GetPlayerInfo(string v)
		{
			Regex regex = new(VideoIdRegex);
			if (!regex.IsMatch(v) || v.Length != 11)
				return GetErrorVideoPlayer(v, "Invalid YouTube ID " + v);

			try
			{
				YoutubePlayer player = await _youtube.GetPlayerAsync(v);
				XmlDocument xml = player.GetXmlDocument();
				return Xml(xml);
			}
			catch (YtDlpException e)
			{
				return GetErrorVideoPlayer(v, e.ErrorMessage.Split(": ").Last());
			}
			catch (Exception e)
			{
				return GetErrorVideoPlayer(v, e.Message);
			}
		}

		private IActionResult GetErrorVideoPlayer(string videoId, string message)
		{
			YoutubePlayer player = new()
			{
				Id = videoId,
				Title = "",
				Description = "",
				Categories = Array.Empty<string>(),
				Tags = Array.Empty<string>(),
				Channel = new Channel
				{
					Name = "",
					Id = "",
					Avatars = Array.Empty<Thumbnail>()
				},
				UploadDate = "1970-01-01",
				Duration = 0,
				Chapters = Array.Empty<Chapter>(),
				Thumbnails = Array.Empty<Thumbnail>(),
				Formats = Array.Empty<Format>(),
				AdaptiveFormats = Array.Empty<Format>(),
				Subtitles = Array.Empty<Subtitle>(),
				Storyboards = Array.Empty<Format>(),
				ExpiresInSeconds = "0",
				ErrorMessage = message
			};
			return Xml(player.GetXmlDocument());
		}

		[Route("video")]
		public async Task<IActionResult> GetVideoInfo(string v)
		{
			Regex regex = new(VideoIdRegex);
			if (!regex.IsMatch(v) || v.Length != 11)
			{
				XmlDocument doc = new();
				XmlElement item = doc.CreateElement("Error");

				item.InnerText = "Invalid YouTube ID " + v;

				doc.AppendChild(item);
				return Xml(doc);
			}

			YoutubeVideo player = await _youtube.GetVideoAsync(v);
			XmlDocument xml = player.GetXmlDocument();
			return Xml(xml);
		}
	}
}