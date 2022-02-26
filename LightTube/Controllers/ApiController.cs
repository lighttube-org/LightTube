using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InnerTube;
using InnerTube.Models;
using InnerTube.Models.YtDlp;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers
{
	[Route("/api")]
	public class ApiController : Controller
	{
		public const string VideoIdRegex = @"[a-zA-Z0-9_-]{11}";
		
		[Route("get_player_info")]
		public async Task<IActionResult> GetPlayerInfo(string v)
		{
			Regex regex = new(VideoIdRegex);
			if (!regex.IsMatch(v) || v.Length != 11)
				return GetErrorVideoPlayer(v, "Invalid YouTube ID " + v);

			try
			{
				YtDlpOutput video = YtDlp.GetVideo(v);
				return Json(await video.GetYoutubePlayer());
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
		
		private IActionResult GetErrorVideoPlayer(string videoId, string message) => Json(new YoutubePlayer
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
				Avatars = Array.Empty<InnerTube.Models.Thumbnail>()
			},
			UploadDate = "1970-01-01",
			Duration = 0,
			Chapters = Array.Empty<InnerTube.Models.Chapter>(),
			Thumbnails = Array.Empty<InnerTube.Models.Thumbnail>(),
			Formats = Array.Empty<InnerTube.Models.Format>(),
			AdaptiveFormats = Array.Empty<InnerTube.Models.Format>(),
			Subtitles = Array.Empty<InnerTube.Models.Subtitle>(),
			Storyboards = Array.Empty<InnerTube.Models.Format>(),
			ExpiresInSeconds = "0",
			ErrorMessage = message
		});
	}
}