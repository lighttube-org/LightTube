using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using InnerTube;
using InnerTube.Models;
using InnerTube.Models.YtDlp;
using Microsoft.AspNetCore.Mvc;
using Chapter = InnerTube.Models.Chapter;
using Format = InnerTube.Models.Format;
using Subtitle = InnerTube.Models.Subtitle;
using Thumbnail = InnerTube.Models.Thumbnail;

namespace LightTube.Controllers
{
	[Route("/api")]
	public class ApiController : Controller
	{
		public const string VideoIdRegex = @"[a-zA-Z0-9_-]{11}";

		private IActionResult Xml(XmlDocument xmlDocument)
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
				YtDlpOutput video = YtDlp.GetVideo(v);
				XmlDocument xml = (await video.GetYoutubePlayer()).GetXmlDocument();
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
	}
}