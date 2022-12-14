using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Xml;
using InnerTube;
using LightTube.Database;
using LightTube.Database.Models;
using Microsoft.Extensions.Primitives;

namespace LightTube;

public static class Utils
{
	private static string? _version;
	public static string UserIdAlphabet => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

	public static string GetRegion(this HttpContext context) =>
		context.Request.Headers.TryGetValue("X-Content-Region", out StringValues h) 
			? h.ToString() 
			: context.Request.Cookies.TryGetValue("gl", out string region)
				? region 
				: Configuration.GetVariable("LIGHTTUBE_DEFAULT_CONTENT_REGION", "US");

	public static string GetLanguage(this HttpContext context) =>
		context.Request.Headers.TryGetValue("X-Content-Language", out StringValues h) 
			? h.ToString() 
			: context.Request.Cookies.TryGetValue("hl", out string language)
				? language
				: Configuration.GetVariable("LIGHTTUBE_DEFAULT_CONTENT_LANGUAGE", "en");

	public static string GetVersion()
	{
		if (_version is null)
		{
#if DEBUG
			DateTime buildTime = DateTime.Today;
			_version = $"{buildTime.Year}.{buildTime.Month}.{buildTime.Day} (dev)";
#else
			_version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location)
				.FileVersion?[2..];
#endif
		}

		return _version;
	}

	public static string GetCodecFromMimeType(string mime)
	{
		try
		{
			return mime.Split("codecs=\"")[1].Replace("\"", "");
		}
		catch
		{
			return "";
		}
	}

	public static async Task<string> GetProxiedHlsManifest(string url, string? proxyRoot = null)
	{
		if (!url.StartsWith("http://") && !url.StartsWith("https://"))
			url = "https://" + url;

		HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
		request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

		using HttpWebResponse response = (HttpWebResponse)request.GetResponse();

		await using Stream stream = response.GetResponseStream();
		using StreamReader reader = new(stream);
		string manifest = await reader.ReadToEndAsync();
		StringBuilder proxyManifest = new();

		List<string> types = new();

		if (proxyRoot is not null)
			foreach (string s in manifest.Split("\n"))
			{
				string? manifestType = null;
				string? manifestUrl = null;

				if (s.StartsWith("https://www.youtube.com/api/timedtext"))
				{
					manifestUrl = s;
					manifestType = "caption";
				}
				else if (s.Contains(".googlevideo.com/videoplayback"))
				{
					manifestType = "segment";
					manifestUrl = s;
				}
				else if (s.StartsWith("http"))
				{
					manifestUrl = s;
					manifestType = s[46..].Split("/")[0];
				}
				else if (s.StartsWith("#EXT-X-MEDIA:URI="))
				{
					manifestUrl = s[18..].Split('"')[0];
					manifestType = s[64..].Split("/")[0];
				}

				string? proxiedUrl = null;

				if (manifestUrl != null)
				{
					switch (manifestType)
					{
						case "hls_playlist":
							// MPEG-TS playlist
							proxiedUrl = "/hls/playlist/" +
							             HttpUtility.UrlEncode(manifestUrl.Split(manifestType)[1]);
							break;
						case "hls_timedtext_playlist":
							// subtitle playlist
							proxiedUrl = "/hls/timedtext/" +
							             HttpUtility.UrlEncode(manifestUrl.Split(manifestType)[1]);
							break;
						case "caption":
							// subtitles
							NameValueCollection qs = HttpUtility.ParseQueryString(manifestUrl.Split("?")[1]);
							proxiedUrl = $"/caption/{qs.Get("v")}/{qs.Get("lang")}";
							break;
						case "segment":
							// HLS segment
							proxiedUrl = "/hls/segment/" +
							             HttpUtility.UrlEncode(manifestUrl.Split("://")[1]);
							break;
					}
				}

				types.Add(manifestType);

				proxyManifest.AppendLine(proxiedUrl is not null && manifestUrl is not null
					//TODO: check if http or https
					? s.Replace(manifestUrl, proxyRoot + proxiedUrl)
					: s);
			}
		else
			proxyManifest.Append(manifest);


		return proxyManifest.ToString();
	}

	public static string GetDashManifest(InnerTubePlayer player, string? proxyUrl)
	{
		XmlDocument doc = new();

		XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
		doc.InsertBefore(xmlDeclaration, doc.DocumentElement);

		XmlElement mpdRoot = doc.CreateElement("MPD");
		mpdRoot.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
		mpdRoot.SetAttribute("xmlns", "urn:mpeg:dash:schema:mpd:2011");
		mpdRoot.SetAttribute("xsi:schemaLocation", "urn:mpeg:dash:schema:mpd:2011 DASH-MPD.xsd");
		mpdRoot.SetAttribute("profiles", "urn:mpeg:dash:profile:isoff-main:2011");
		mpdRoot.SetAttribute("type", "static");
		mpdRoot.SetAttribute("minBufferTime", "PT1.500S");
		StringBuilder duration = new("PT");
		if (player.Details.Length.TotalHours > 0)
			duration.Append($"{player.Details.Length.Hours}H");
		if (player.Details.Length.Minutes > 0)
			duration.Append($"{player.Details.Length.Minutes}M");
		if (player.Details.Length.Seconds > 0)
			duration.Append(player.Details.Length.Seconds);
		mpdRoot.SetAttribute("mediaPresentationDuration", $"{duration}.{player.Details.Length.Milliseconds}S");
		doc.AppendChild(mpdRoot);

		XmlElement period = doc.CreateElement("Period");

		period.AppendChild(doc.CreateComment("Audio Adaptation Set"));
		XmlElement audioAdaptationSet = doc.CreateElement("AdaptationSet");
		List<Format> audios = player.AdaptiveFormats
			.Where(x => x.AudioChannels.HasValue)
			.ToList();

		audioAdaptationSet.SetAttribute("mimeType",
			HttpUtility.ParseQueryString(audios.First().Url.Query).Get("mime"));
		audioAdaptationSet.SetAttribute("subsegmentAlignment", "true");
		audioAdaptationSet.SetAttribute("contentType", "audio");
		foreach (Format format in audios)
		{
			XmlElement representation = doc.CreateElement("Representation");
			representation.SetAttribute("id", format.Itag);
			representation.SetAttribute("codecs", GetCodecFromMimeType(format.MimeType));
			representation.SetAttribute("startWithSAP", "1");
			representation.SetAttribute("bandwidth", format.Bitrate.ToString());

			XmlElement audioChannelConfiguration = doc.CreateElement("AudioChannelConfiguration");
			audioChannelConfiguration.SetAttribute("schemeIdUri",
				"urn:mpeg:dash:23003:3:audio_channel_configuration:2011");
			audioChannelConfiguration.SetAttribute("value", "2");
			representation.AppendChild(audioChannelConfiguration);

			XmlElement baseUrl = doc.CreateElement("BaseURL");
			baseUrl.InnerText = string.IsNullOrWhiteSpace(proxyUrl)
				? format.Url.ToString()
				: $"{proxyUrl}/media/{player.Details.Id}/{format.Itag}";
			representation.AppendChild(baseUrl);

			if (format.IndexRange != null && format.InitRange != null)
			{
				XmlElement segmentBase = doc.CreateElement("SegmentBase");
				segmentBase.SetAttribute("indexRange", $"{format.IndexRange.Start}-{format.IndexRange.End}");
				segmentBase.SetAttribute("indexRangeExact", "true");

				XmlElement initialization = doc.CreateElement("Initialization");
				initialization.SetAttribute("range", $"{format.InitRange.Start}-{format.InitRange.End}");

				segmentBase.AppendChild(initialization);
				representation.AppendChild(segmentBase);
			}

			audioAdaptationSet.AppendChild(representation);
		}

		period.AppendChild(audioAdaptationSet);

		period.AppendChild(doc.CreateComment("Video Adaptation Set"));

		List<Format> videos = player.AdaptiveFormats.Where(x => !x.AudioChannels.HasValue).ToList();

		XmlElement videoAdaptationSet = doc.CreateElement("AdaptationSet");
		videoAdaptationSet.SetAttribute("mimeType",
			HttpUtility.ParseQueryString(videos.FirstOrDefault()?.Url.Query ?? "mime=video/mp4")
				.Get("mime"));
		videoAdaptationSet.SetAttribute("subsegmentAlignment", "true");
		videoAdaptationSet.SetAttribute("contentType", "video");

		foreach (Format format in videos)
		{
			XmlElement representation = doc.CreateElement("Representation");
			representation.SetAttribute("id", format.Itag);
			representation.SetAttribute("codecs", GetCodecFromMimeType(format.MimeType));
			representation.SetAttribute("startWithSAP", "1");
			representation.SetAttribute("width", format.Width.ToString());
			representation.SetAttribute("height", format.Height.ToString());
			representation.SetAttribute("bandwidth", format.Bitrate.ToString());

			XmlElement baseUrl = doc.CreateElement("BaseURL");
			baseUrl.InnerText = string.IsNullOrWhiteSpace(proxyUrl)
				? format.Url.ToString()
				: $"{proxyUrl}/media/{player.Details.Id}/{format.Itag}";
			representation.AppendChild(baseUrl);

			if (format.IndexRange != null && format.InitRange != null)
			{
				XmlElement segmentBase = doc.CreateElement("SegmentBase");
				segmentBase.SetAttribute("indexRange", $"{format.IndexRange.Start}-{format.IndexRange.End}");
				segmentBase.SetAttribute("indexRangeExact", "true");

				XmlElement initialization = doc.CreateElement("Initialization");
				initialization.SetAttribute("range", $"{format.InitRange.Start}-{format.InitRange.End}");

				segmentBase.AppendChild(initialization);
				representation.AppendChild(segmentBase);
			}

			videoAdaptationSet.AppendChild(representation);
		}

		period.AppendChild(videoAdaptationSet);

		period.AppendChild(doc.CreateComment("Subtitle Adaptation Sets"));
		foreach (InnerTubePlayer.VideoCaption subtitle in player.Captions)
		{
			period.AppendChild(doc.CreateComment(subtitle.Label));
			XmlElement adaptationSet = doc.CreateElement("AdaptationSet");
			adaptationSet.SetAttribute("mimeType", "text/vtt");
			adaptationSet.SetAttribute("lang", subtitle.LanguageCode);

			XmlElement representation = doc.CreateElement("Representation");
			representation.SetAttribute("id", $"caption_{subtitle.LanguageCode.ToLower()}");
			representation.SetAttribute("bandwidth", "256"); // ...why do we need this for a plaintext file

			XmlElement baseUrl = doc.CreateElement("BaseURL");
			string url = subtitle.BaseUrl.ToString();
			url = url.Replace("fmt=srv3", "fmt=vtt");
			baseUrl.InnerText = string.IsNullOrWhiteSpace(proxyUrl)
				? url
				: $"{proxyUrl}/caption/{player.Details.Id}/{subtitle.LanguageCode}";

			representation.AppendChild(baseUrl);
			adaptationSet.AppendChild(representation);
			period.AppendChild(adaptationSet);
		}

		mpdRoot.AppendChild(period);
		return doc.OuterXml;
	}

	public static string ToKMB(this int num) =>
		num switch
		{
			> 999999999 or < -999999999 => num.ToString("0,,,.###B", CultureInfo.InvariantCulture),
			> 999999 or < -999999 => num.ToString("0,,.##M", CultureInfo.InvariantCulture),
			> 999 or < -999 => num.ToString("0,.#K", CultureInfo.InvariantCulture),
			var _ => num.ToString(CultureInfo.InvariantCulture)
		};

	public static string ToDurationString(this TimeSpan ts)
	{
		string str = ts.ToString();
		return str.StartsWith("00:") ? str[3..] : str;
	}

	public static string GetContinuationUrl(string currentPath, string contToken)
	{
		string[] parts = currentPath.Split("?");
		NameValueCollection query = parts.Length > 1
			? HttpUtility.ParseQueryString(parts[1])
			: new NameValueCollection();
		query.Set("continuation", contToken);
		return $"{parts[0]}?{query.AllKeys.Select(x => x + "=" + query.Get(x)).Aggregate((a, b) => $"{a}&{b}")}";
	}

	public static SubscriptionType GetSubscriptionType(this HttpContext context, string? channelId)
	{
		if (channelId is null) return SubscriptionType.NONE;
		DatabaseUser? user = DatabaseManager.Users.GetUserFromToken(context.Request.Cookies["token"] ?? "").Result;
		if (user is null) return SubscriptionType.NONE;
		return user.Subscriptions.TryGetValue(channelId, out SubscriptionType type) ? type : SubscriptionType.NONE;
	}
}