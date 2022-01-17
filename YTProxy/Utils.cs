using System.Text.RegularExpressions;

namespace YTProxy
{
	public class Utils
	{
		public static string GetHtmlDescription(string description)
		{
			const string urlPattern = @"(http[s]*)://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";
			const string hashtagPattern = @"#[\w]*";
			string html = description.Replace("\n", "<br>");

			// turn URLs into hyperlinks
			Regex urlRegex = new(urlPattern, RegexOptions.IgnoreCase);
			Match m;
			for (m = urlRegex.Match(html); m.Success; m = m.NextMatch())
				html = html.Replace(m.Groups[0].ToString(),
					$"<a href=\"{m.Groups[0]}\">{m.Groups[0]}</a>");

			// turn hashtags into hyperlinks
			Regex chr = new(hashtagPattern, RegexOptions.IgnoreCase);
			for (m = chr.Match(html); m.Success; m = m.NextMatch())
				html = html.Replace(m.Groups[0].ToString(),
					$"<a href=\"/hashtag/{m.Groups[0].ToString().Replace("#", "")}\">{m.Groups[0]}</a>");
			return html;
		}
	}
}