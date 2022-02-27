using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using YTProxy.Models;

namespace YTProxy
{
	public static class Utils
	{
		public static string GetHtmlDescription(string description)
		{
			const string urlPattern = @"(http[s]*)://([\w-]+\.)+[\w-]+(\S)*";
			const string hashtagPattern = @"#[\w]*";
			string html = description.Replace("\n", " <br> ");

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

		public static string GetMpdManifest(this YoutubePlayer player, string proxyUrl)
		{
			XmlDocument doc = new();

			XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
			XmlElement root = doc.DocumentElement;
			doc.InsertBefore(xmlDeclaration, root);

			XmlElement mpdRoot = doc.CreateElement(string.Empty, "MPD", string.Empty);
			mpdRoot.SetAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
			mpdRoot.SetAttribute("xmlns", "urn:mpeg:dash:schema:mpd:2011");
			mpdRoot.SetAttribute("xsi:schemaLocation", "urn:mpeg:dash:schema:mpd:2011 DASH-MPD.xsd");
			mpdRoot.SetAttribute("profiles", "urn:mpeg:dash:profile:isoff-main:2011");
			mpdRoot.SetAttribute("type", "static");
			mpdRoot.SetAttribute("minBufferTime", "PT1.500S");
			TimeSpan durationTs = TimeSpan.FromSeconds(double.Parse(HttpUtility
				.ParseQueryString(player.AdaptiveFormats.First(x => x.Resolution == "audio only").Url.Query)
				.Get("dur") ?? "0"));
			StringBuilder duration = new("PT");
			if (durationTs.TotalHours > 0)
				duration.Append($"{durationTs.Hours}H");
			if (durationTs.Minutes > 0)
				duration.Append($"{durationTs.Minutes}M");
			if (durationTs.Seconds > 0)
				duration.Append(durationTs.Seconds);
			mpdRoot.SetAttribute("mediaPresentationDuration", $"{duration}.{durationTs.Milliseconds}S");
			doc.AppendChild(mpdRoot);

			XmlElement period = doc.CreateElement("Period");

			period.AppendChild(doc.CreateComment("Audio Adaptation Set"));
			XmlElement audioAdaptationSet = doc.CreateElement("AdaptationSet");
			List<Format> audios = player.AdaptiveFormats
				.Where(x => x.Resolution == "audio only" && x.InitRange is not null && x.IndexRange is not null)
				.GroupBy(x => x.FormatNote)
				.Select(x => x.Last())
				.ToList();
			audioAdaptationSet.SetAttribute("mimeType",
				HttpUtility.ParseQueryString(audios.First().Url.Query).Get("mime"));
			audioAdaptationSet.SetAttribute("subsegmentAlignment", "true");
			audioAdaptationSet.SetAttribute("contentType", "audio");
			foreach (Format format in audios)
			{
				XmlElement representation = doc.CreateElement("Representation");
				representation.SetAttribute("id", format.FormatId);
				representation.SetAttribute("codecs", format.AudioCodec);
				representation.SetAttribute("startWithSAP", "1");
				representation.SetAttribute("bandwidth",
					Math.Floor((format.Filesize ?? 1) / (double)player.Duration).ToString());

				XmlElement audioChannelConfiguration = doc.CreateElement("AudioChannelConfiguration");
				audioChannelConfiguration.SetAttribute("schemeIdUri",
					"urn:mpeg:dash:23003:3:audio_channel_configuration:2011");
				audioChannelConfiguration.SetAttribute("value", "2");
				representation.AppendChild(audioChannelConfiguration);

				XmlElement baseUrl = doc.CreateElement("BaseURL");
				if (string.IsNullOrWhiteSpace(proxyUrl))
					baseUrl.InnerText = format.Url.ToString();
				else
					baseUrl.InnerText = proxyUrl + HttpUtility.UrlEncode(format.Url.ToString());
				representation.AppendChild(baseUrl);

				XmlElement segmentBase = doc.CreateElement("SegmentBase");
				segmentBase.SetAttribute("indexRange", $"{format.IndexRange.Start}-{format.IndexRange.End}");
				segmentBase.SetAttribute("indexRangeExact", "true");

				XmlElement initialization = doc.CreateElement("Initialization");
				initialization.SetAttribute("range", $"{format.InitRange.Start}-{format.InitRange.End}");

				segmentBase.AppendChild(initialization);
				representation.AppendChild(segmentBase);

				audioAdaptationSet.AppendChild(representation);
			}

			period.AppendChild(audioAdaptationSet);

			period.AppendChild(doc.CreateComment("Video Adaptation Set"));
			XmlElement videoAdaptationSet = doc.CreateElement("AdaptationSet");
			videoAdaptationSet.SetAttribute("mimeType",
				HttpUtility.ParseQueryString(player.AdaptiveFormats.Last(x => x.Resolution != "audio only").Url.Query)
					.Get("mime"));
			videoAdaptationSet.SetAttribute("subsegmentAlignment", "true");
			videoAdaptationSet.SetAttribute("contentType", "video");
			foreach (Format format in player.AdaptiveFormats
				.Where(x => x.Resolution != "audio only" && x.InitRange is not null && x.IndexRange is not null)
				.GroupBy(x => x.FormatNote)
				.Select(x => x.Last())
				.ToList())
			{
				XmlElement representation = doc.CreateElement("Representation");
				representation.SetAttribute("id", format.FormatId);
				representation.SetAttribute("codecs", format.VideoCodec);
				representation.SetAttribute("startWithSAP", "1");
				string[] widthAndHeight = format.Resolution.Split("x");
				representation.SetAttribute("width", widthAndHeight[0]);
				representation.SetAttribute("height", widthAndHeight[1]);
				representation.SetAttribute("bandwidth",
					Math.Floor((format.Filesize ?? 1) / (double)player.Duration).ToString());

				XmlElement baseUrl = doc.CreateElement("BaseURL");
				if (string.IsNullOrWhiteSpace(proxyUrl))
					baseUrl.InnerText = format.Url.ToString();
				else
					baseUrl.InnerText = proxyUrl + HttpUtility.UrlEncode(format.Url.ToString());
				representation.AppendChild(baseUrl);

				XmlElement segmentBase = doc.CreateElement("SegmentBase");
				segmentBase.SetAttribute("indexRange", $"{format.IndexRange.Start}-{format.IndexRange.End}");
				segmentBase.SetAttribute("indexRangeExact", "true");

				XmlElement initialization = doc.CreateElement("Initialization");
				initialization.SetAttribute("range", $"{format.InitRange.Start}-{format.InitRange.End}");

				segmentBase.AppendChild(initialization);
				representation.AppendChild(segmentBase);

				videoAdaptationSet.AppendChild(representation);
			}

			period.AppendChild(videoAdaptationSet);

			mpdRoot.AppendChild(period);
			return doc.OuterXml.Replace(" schemaLocation=\"", " xsi:schemaLocation=\"");
		}
	}
}