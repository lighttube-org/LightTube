using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using YTProxy.Models;

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

		public static string GetMpdManifest(YoutubePlayer player, string proxyUrl)
		{
			XmlDocument doc = new();

			XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
			XmlElement root = doc.DocumentElement;
			doc.InsertBefore(xmlDeclaration, root);

			XmlElement mpdRoot = doc.CreateElement(string.Empty, "MPD", string.Empty);
			mpdRoot.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
			mpdRoot.SetAttribute("xmlns", "urn:mpeg:DASH:schema:MPD:2011");
			mpdRoot.SetAttribute("xsi:schemaLocation", "urn:mpeg:DASH:schema:MPD:2011 DASH-MPD.xsd");
			mpdRoot.SetAttribute("profiles", "urn:mpeg:dash:profile:isoff-on-demand:2011");
			mpdRoot.SetAttribute("type", "static");
			mpdRoot.SetAttribute("minBufferTime", "PT1.500S");
			TimeSpan durationTs = TimeSpan.FromSeconds(double.Parse(HttpUtility.ParseQueryString(player.AdaptiveFormats.First(x => x.Resolution == "audio only").Url.Query).Get("dur") ?? "0"));
			StringBuilder duration = new("PT");
			if (durationTs.TotalHours > 0)
				duration.Append($"{durationTs.TotalHours}H");
			if (durationTs.Minutes > 0)
				duration.Append($"{durationTs.Minutes}M");
			if (durationTs.Seconds > 0)
				duration.Append(durationTs.Seconds);
			mpdRoot.SetAttribute("mediaPresentationDuration", $"PT{duration}.{durationTs.Milliseconds}S");
			doc.AppendChild(mpdRoot);

			XmlElement period = doc.CreateElement( "Period");


			XmlElement audioAdaptationSet = doc.CreateElement( "AdaptationSet");
			audioAdaptationSet.SetAttribute("mimeType", HttpUtility.ParseQueryString(player.AdaptiveFormats.First(x => x.Resolution == "audio only").Url.Query).Get("mime"));
			audioAdaptationSet.SetAttribute("subsegmentAlignment", "true");
			foreach (AdaptiveFormat format in player.AdaptiveFormats.Where(x => x.Resolution == "audio only"))
			{
				NameValueCollection query = HttpUtility.ParseQueryString(format.Url.Query);
				XmlElement representation = doc.CreateElement("Representation");
				representation.SetAttribute("id", format.FormatId);
				representation.SetAttribute("codecs", "mp4a.40.5");
				//representation.SetAttribute("audioSamplingRate", "");
				representation.SetAttribute("startWithSAP", "1");
				//representation.SetAttribute("bandwidth", "");

				XmlElement audioChannelConfiguration = doc.CreateElement("AudioChannelConfiguration");
				audioChannelConfiguration.SetAttribute("schemeIdUri", "urn:mpeg:dash:23003:3:audio_channel_configuration:2011");
				audioChannelConfiguration.SetAttribute("value", "2");
				representation.AppendChild(audioChannelConfiguration);

				XmlElement baseUrl = doc.CreateElement("BaseURL");
				baseUrl.InnerText = proxyUrl + HttpUtility.UrlEncode(format.Url.ToString());
				representation.AppendChild(baseUrl);

				audioAdaptationSet.AppendChild(representation);
			}
			period.AppendChild(audioAdaptationSet);
			
			XmlElement videoAdaptationSet = doc.CreateElement( "AdaptationSet");
			videoAdaptationSet.SetAttribute("mimeType", HttpUtility.ParseQueryString(player.AdaptiveFormats.First(x => x.Resolution != "audio only").Url.Query).Get("mime"));
			videoAdaptationSet.SetAttribute("subsegmentAlignment", "true");
			foreach (AdaptiveFormat format in player.AdaptiveFormats.Where(x => x.Resolution != "audio only"))
			{
				XmlElement representation = doc.CreateElement("Representation");
				representation.SetAttribute("id", format.FormatId);
				representation.SetAttribute("codecs", "avc1.4d4015");
				representation.SetAttribute("startWithSAP", "1");
				string[] widthAndHeight = format.Resolution.Split("x");
				representation.SetAttribute("width", widthAndHeight[0]);
				representation.SetAttribute("height", widthAndHeight[1]);
				//representation.SetAttribute("bandwidth", "");

				XmlElement baseUrl = doc.CreateElement("BaseURL");
				baseUrl.InnerText = proxyUrl + HttpUtility.UrlEncode(format.Url.ToString());
				representation.AppendChild(baseUrl);

				videoAdaptationSet.AppendChild(representation);
			}
			period.AppendChild(videoAdaptationSet);

			mpdRoot.AppendChild(period);
			return doc.InnerXml;
		}

		public static string GetHlsManifest(YoutubePlayer player, string proxyUrl)
		{
			TimeSpan durationTs = TimeSpan.FromSeconds(double.Parse(HttpUtility.ParseQueryString(player.AdaptiveFormats.First(x => x.Resolution == "audio only").Url.Query).Get("dur") ?? "0"));
			StringBuilder hls = new();
			
			hls.AppendLine("#EXTM3U");
			hls.AppendLine("#EXT-X-VERSION:3");
			hls.AppendLine("##EXT-X-PLAYLIST-TYPE:VOD");
			hls.AppendLine("#EXT-X-TARGETDURATION:" + (int)durationTs.TotalSeconds);
			//hls.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");

			hls.AppendLine(
				$"#EXT-X-MEDIA:NAME=\"YouTube DASH Audio\", TYPE=AUDIO, GROUP-ID=\"ytdash-audio\", LANGUAGE=\"en\", DEFAULT=YES, AUTOSELECT=YES, URI=\"{proxyUrl + HttpUtility.UrlEncode(player.AdaptiveFormats.Last(x => x.Resolution == "audio only").Url.ToString())}\"");
			AdaptiveFormat format = player.AdaptiveFormats.First(x => x.Resolution != "audio only");
			
			hls.AppendLine($"#EXTINF:{(int)durationTs.TotalSeconds},");
			hls.AppendLine(proxyUrl + HttpUtility.UrlEncode(format.Url.ToString()));
			/*
			StringBuilder duration = new("PT");
			if (durationTs.TotalHours > 0)
				duration.Append($"{durationTs.TotalHours}H");
			if (durationTs.Minutes > 0)
				duration.Append($"{durationTs.Minutes}M");
			if (durationTs.Seconds > 0)
				duration.Append(durationTs.Seconds);
			mpdRoot.SetAttribute("mediaPresentationDuration", $"PT{duration}.{durationTs.Milliseconds}S");
			doc.AppendChild(mpdRoot);

			XmlElement period = doc.CreateElement( "Period");


			XmlElement audioAdaptationSet = doc.CreateElement( "AdaptationSet");
			audioAdaptationSet.SetAttribute("mimeType", HttpUtility.ParseQueryString(player.AdaptiveFormats.First(x => x.Resolution == "audio only").Url.Query).Get("mime"));
			audioAdaptationSet.SetAttribute("subsegmentAlignment", "true");
			foreach (AdaptiveFormat format in player.AdaptiveFormats.Where(x => x.Resolution == "audio only"))
			{
				NameValueCollection query = HttpUtility.ParseQueryString(format.Url.Query);
				XmlElement representation = doc.CreateElement("Representation");
				representation.SetAttribute("id", format.FormatId);
				representation.SetAttribute("codecs", "mp4a.40.5");
				//representation.SetAttribute("audioSamplingRate", "");
				representation.SetAttribute("startWithSAP", "1");
				//representation.SetAttribute("bandwidth", "");

				XmlElement audioChannelConfiguration = doc.CreateElement("AudioChannelConfiguration");
				audioChannelConfiguration.SetAttribute("schemeIdUri", "urn:mpeg:dash:23003:3:audio_channel_configuration:2011");
				audioChannelConfiguration.SetAttribute("value", "2");
				representation.AppendChild(audioChannelConfiguration);

				XmlElement baseUrl = doc.CreateElement("BaseURL");
				baseUrl.InnerText = proxyUrl + HttpUtility.UrlEncode(format.Url.ToString());
				representation.AppendChild(baseUrl);

				audioAdaptationSet.AppendChild(representation);
			}
			period.AppendChild(audioAdaptationSet);
			
			XmlElement videoAdaptationSet = doc.CreateElement( "AdaptationSet");
			videoAdaptationSet.SetAttribute("mimeType", HttpUtility.ParseQueryString(player.AdaptiveFormats.First(x => x.Resolution != "audio only").Url.Query).Get("mime"));
			videoAdaptationSet.SetAttribute("subsegmentAlignment", "true");
			foreach (AdaptiveFormat format in player.AdaptiveFormats.Where(x => x.Resolution != "audio only"))
			{
				XmlElement representation = doc.CreateElement("Representation");
				representation.SetAttribute("id", format.FormatId);
				representation.SetAttribute("codecs", "avc1.4d4015");
				representation.SetAttribute("startWithSAP", "1");
				string[] widthAndHeight = format.Resolution.Split("x");
				representation.SetAttribute("width", widthAndHeight[0]);
				representation.SetAttribute("height", widthAndHeight[1]);
				//representation.SetAttribute("bandwidth", "");

				XmlElement baseUrl = doc.CreateElement("BaseURL");
				baseUrl.InnerText = proxyUrl + HttpUtility.UrlEncode(format.Url.ToString());
				representation.AppendChild(baseUrl);

				videoAdaptationSet.AppendChild(representation);
			}
			period.AppendChild(videoAdaptationSet);

			mpdRoot.AppendChild(period);
			*/
			return hls.ToString();
		}
	}
}