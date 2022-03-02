using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using InnerTube.Models.YtDlp;
using Newtonsoft.Json;

namespace InnerTube
{
	public static class YtDlp
	{
		public static YtDlpOutput GetVideo(string url)
		{
			Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "yt-dlp",
				Arguments = $"--dump-single-json {url}",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				RedirectStandardInput = true
			});
			if (process is null)
				throw new InvalidOperationException("Failed to start yt-dlp! Make sure it is installed and also in the system PATH");
			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();
			if (!string.IsNullOrWhiteSpace(error))
				throw new YtDlpException(error);
			return JsonConvert.DeserializeObject<YtDlpOutput>(output);
		} 
	}

	public class YtDlpException : Exception
	{
		private const string Pattern = @"\[([\S]*)\] ([\s\S]*)";
		public string ErrorNamespace;
		public string ErrorMessage;

		public YtDlpException(string error)
		{
			Regex regex = new(Pattern);
			GroupCollection groups = regex.Match(error).Groups;

			ErrorNamespace = groups[1].Value.Trim();
			ErrorMessage = groups[2].Value.Trim();
		}
	}
}