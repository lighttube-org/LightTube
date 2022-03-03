using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using InnerTube.Models;
using Newtonsoft.Json.Linq;

namespace InnerTube
{
	public static class Utils
	{
		public static string GetHtmlDescription(string description) => description.Replace("\n", "<br>");

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
			//mpdRoot.SetAttribute("profiles", "urn:mpeg:dash:profile:isoff-on-demand:2011");
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
				.Where(x => x.Resolution == "audio only")
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
			XmlElement videoAdaptationSet = doc.CreateElement("AdaptationSet");
			videoAdaptationSet.SetAttribute("mimeType",
				HttpUtility.ParseQueryString(player.AdaptiveFormats.Last(x => x.Resolution != "audio only").Url.Query)
					.Get("mime"));
			videoAdaptationSet.SetAttribute("subsegmentAlignment", "true");
			videoAdaptationSet.SetAttribute("contentType", "video");
			foreach (Format format in player.AdaptiveFormats.Where(x => x.Resolution != "audio only")
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
			foreach (Subtitle subtitle in player.Subtitles ?? Array.Empty<Subtitle>())
			{
				period.AppendChild(doc.CreateComment(subtitle.Language));
				XmlElement adaptationSet = doc.CreateElement("AdaptationSet");
				adaptationSet.SetAttribute("mimeType", "text/vtt");
				adaptationSet.SetAttribute("lang", subtitle.Language);

				XmlElement representation = doc.CreateElement("Representation");
				representation.SetAttribute("id", $"caption_{subtitle.Language.ToLower()}");
				representation.SetAttribute("bandwidth", "256"); // ...why do we need this for a plaintext file
				
				XmlElement baseUrl = doc.CreateElement("BaseURL");
				if (string.IsNullOrWhiteSpace(proxyUrl))
					baseUrl.InnerText = subtitle.Url.ToString();
				else
					baseUrl.InnerText = proxyUrl + HttpUtility.UrlEncode(subtitle.Url.ToString());

				representation.AppendChild(baseUrl);
				adaptationSet.AppendChild(representation);
				period.AppendChild(adaptationSet);
			}

			mpdRoot.AppendChild(period);
			return doc.OuterXml.Replace(" schemaLocation=\"", " xsi:schemaLocation=\"");
		}

		public static string ReadRuns(JArray runs)
		{
			string str = "";
			foreach (JToken runToken in runs ?? new JArray())
			{
				JObject run = runToken as JObject;
				if (run is null) continue;

				if (run.ContainsKey("bold"))
				{
					str += "<b>" + run["text"] + "</b>";
				}
				else if (run.ContainsKey("navigationEndpoint"))
				{
					if (run?["navigationEndpoint"]?["urlEndpoint"] is not null)
					{
						string url = run["navigationEndpoint"]?["urlEndpoint"]?["url"]?.ToString() ?? "";
						if (url.StartsWith("https://www.youtube.com/redirect"))
						{
							NameValueCollection qsl = HttpUtility.ParseQueryString(url.Split("?")[1]);
							url = qsl["url"] ?? qsl["q"];
						}

						str += $"<a href=\"{url}\">{run["text"]}</a>";
					}
					else if (run?["navigationEndpoint"]?["commandMetadata"] is not null)
					{
						string url = run["navigationEndpoint"]?["commandMetadata"]?["webCommandMetadata"]?["url"]
							?.ToString() ?? "";
						if (url.StartsWith("/"))
							url = "https://youtube.com" + url;
						str += $"<a href=\"{url}\">{run["text"]}</a>";
					}
				}
				else
				{
					str += run["text"];
				}
			}

			return str;
		}

		public static Thumbnail ParseThumbnails(JToken arg) => new()
		{
			Height = arg["height"]?.ToObject<long>() ?? -1,
			Url = new Uri(arg["url"]?.ToString() ?? string.Empty),
			Width = arg["width"]?.ToObject<long>() ?? -1
		};

		public static async Task<JObject> GetAuthorizedPlayer(string id, HttpClient client)
		{
			string SAPISID = Environment.GetEnvironmentVariable("SAPISID");
			string PSID = Environment.GetEnvironmentVariable("PSID");
			
			HttpRequestMessage hrm = new(HttpMethod.Post,
				"https://www.youtube.com/youtubei/v1/player?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8");
			
			byte[] buffer = Encoding.UTF8.GetBytes(
				RequestContext.BuildRequestContextJson(new Dictionary<string, object>
				{
					["videoId"] = id
				}));
			ByteArrayContent byteContent = new(buffer);
			byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			hrm.Content = byteContent;
			
			if (SAPISID is not null && PSID is not null)
			{
				Console.WriteLine("Using the authorized /player endpoint");
				hrm.Headers.Add("Cookie", $"SAPISID={SAPISID}; __Secure-3PAPISID={SAPISID}; __Secure-3PSID={PSID};");
				hrm.Headers.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:96.0) Gecko/20100101 Firefox/96.0");
				hrm.Headers.Add("Authorization", GenerateAuthHeader(SAPISID));
				hrm.Headers.Add("X-Origin", "https://www.youtube.com");
				hrm.Headers.Add("X-Youtube-Client-Name", "1");
				hrm.Headers.Add("X-Youtube-Client-Version", "2.20210721.00.00");
				hrm.Headers.Add("Accept-Language", "en-US;q=0.8,en;q=0.7");
				hrm.Headers.Add("Origin", "https://www.youtube.com");
				hrm.Headers.Add("Referer", "https://www.youtube.com/watch?v=" + id);
			}

			HttpResponseMessage ytPlayerRequest = await client.SendAsync(hrm);
			return JObject.Parse(await ytPlayerRequest.Content.ReadAsStringAsync());
		}

		private static string GenerateAuthHeader(string sapisid)
		{
			long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			string hashInput = timestamp + " " + sapisid + " https://www.youtube.com";
			string hashDigest = GenerateSha1Hash(hashInput);
			return $"SAPISIDHASH {timestamp}_{hashDigest}";
		}

		private static string GenerateSha1Hash(string input)
		{
			using SHA1Managed sha1 = new();
			byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
			StringBuilder sb = new(hash.Length * 2);
			foreach (byte b in hash) sb.Append(b.ToString("X2"));
			return sb.ToString();
		}
	}
}