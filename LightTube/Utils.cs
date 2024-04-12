using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;
using InnerTube;
using LightTube.Database;
using LightTube.Database.Models;
using LightTube.Localization;
using Microsoft.Extensions.Primitives;

namespace LightTube;

public static class Utils
{
    private static string? _version;
    private static string? _itVersion;
    public static string UserIdAlphabet => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    public static string[] OauthScopes =
    [
        "playlists.read",
        "playlists.write",
        "subscriptions.read",
        "subscriptions.write"
    ];

    public static string GetInnerTubeRegion(this HttpContext context) =>
        context.Request.Headers.TryGetValue("X-Content-Region", out StringValues h)
            ? h.ToString()
            : context.Request.Cookies.TryGetValue("gl", out string region)
                ? region
                : Configuration.DefaultContentRegion;

    public static string GetInnerTubeLanguage(this HttpContext context) =>
        context.Request.Headers.TryGetValue("X-Content-Language", out StringValues h)
            ? h.ToString()
            : context.Request.Cookies.TryGetValue("hl", out string language) && language != "localized"
                ? language
                : LocalizationManager.GetFromHttpContext(context).GetRawString("language.innertube");

    public static bool IsInnerTubeLanguageLocalized(this HttpContext context) =>
        context.Request.Cookies.TryGetValue("hl", out string language) ? language == "localized" : true;

    public static bool GetDefaultRecommendationsVisibility(this HttpContext context) =>
        context.Request.Cookies.TryGetValue("recommendations", out string recommendations)
            ? recommendations == "visible"
            : true;

    public static bool GetDefaultCompatibility(this HttpContext context) =>
        context.Request.Cookies.TryGetValue("compatibility", out string compatibility)
            ? compatibility == "true"
            : false;

    public static string GetVersion()
    {
        if (_version is null)
        {
#if DEBUG
            DateTime buildTime = DateTime.Today;
            _version = $"{buildTime.Year}.{buildTime.Month}.{buildTime.Day} (dev)";
#else
			_version = Assembly.GetExecutingAssembly().GetName().Version!.ToString()[2..];
#endif
        }

        return _version;
    }

    public static string GetInnerTubeVersion()
    {
        _itVersion ??= typeof(InnerTube.InnerTube).Assembly.GetName().Version!.ToString();

        return _itVersion;
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

    public static async Task<string> GetProxiedHlsManifest(string url, string? proxyRoot = null,
        bool skipCaptions = false)
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

        List<string> types = [];

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

                if (skipCaptions && manifestType == "caption") continue;
                proxyManifest.AppendLine(proxiedUrl is not null && manifestUrl is not null
                    //TODO: check if http or https
                    ? s.Replace(manifestUrl, proxyRoot + proxiedUrl)
                    : s);
            }
        else
            proxyManifest.Append(manifest);


        return proxyManifest.ToString();
    }

    public static string GetDashManifest(InnerTubePlayer player, string? proxyUrl = null, bool skipCaptions = false)
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

        period.AppendChild(doc.CreateComment("Audio Adaptation Sets"));
        List<Format> audios = player.AdaptiveFormats
            .Where(x => x.AudioChannels.HasValue)
            .ToList();
        IEnumerable<IGrouping<string?, Format>> grouped = audios.GroupBy(x => x.AudioTrack?.Id);
        foreach (IGrouping<string?, Format> formatGroup in grouped.OrderBy(x => x.First().AudioTrack?.AudioIsDefault))
        {
            XmlElement audioAdaptationSet = doc.CreateElement("AdaptationSet");

            audioAdaptationSet.SetAttribute("mimeType",
                HttpUtility.ParseQueryString(audios.First().Url.Query).Get("mime"));
            audioAdaptationSet.SetAttribute("subsegmentAlignment", "true");
            audioAdaptationSet.SetAttribute("contentType", "audio");
            audioAdaptationSet.SetAttribute("lang", formatGroup.Key);

            if (formatGroup.First().AudioTrack != null)
            {
                XmlElement label = doc.CreateElement("Label");
                label.InnerText = formatGroup.First().AudioTrack?.DisplayName;
                audioAdaptationSet.AppendChild(label);
            }

            foreach (Format format in formatGroup)
            {
                XmlElement representation = doc.CreateElement("Representation");
                representation.SetAttribute("id", format.Itag);
                representation.SetAttribute("codecs", GetCodecFromMimeType(format.MimeType));
                representation.SetAttribute("startWithSAP", "1");
                representation.SetAttribute("bandwidth", format.Bitrate.ToString());

                XmlElement audioChannelConfiguration = doc.CreateElement("AudioChannelConfiguration");
                audioChannelConfiguration.SetAttribute("schemeIdUri",
                    "urn:mpeg:dash:23003:3:audio_channel_configuration:2011");
                audioChannelConfiguration.SetAttribute("value", format.AudioChannels?.ToString());
                representation.AppendChild(audioChannelConfiguration);

                XmlElement baseUrl = doc.CreateElement("BaseURL");
                baseUrl.InnerText = string.IsNullOrWhiteSpace(proxyUrl)
                    ? format.Url.ToString()
                    : $"{proxyUrl}/media/{player.Details.Id}/{format.Itag}?audioTrackId={format.AudioTrack?.Id}";
                representation.AppendChild(baseUrl);

                if (format.IndexRange != null && format.InitRange != null)
                {
                    XmlElement segmentBase = doc.CreateElement("SegmentBase");
                    // sometimes wrong?? idk
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
        }

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

        if (!skipCaptions)
        {
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
            : [];
        query.Set("continuation", contToken);
        return $"{parts[0]}?{query.AllKeys.Select(x => x + "=" + query.Get(x)).Aggregate((a, b) => $"{a}&{b}")}";
    }

    public static string GetSkipUrl(string currentPath, int skipAmount)
    {
        string[] parts = currentPath.Split("?");
        NameValueCollection query = parts.Length > 1
            ? HttpUtility.ParseQueryString(parts[1])
            : [];
        query.Set("skip", skipAmount.ToString());
        return $"{parts[0]}?{query.AllKeys.Select(x => x + "=" + query.Get(x)).Aggregate((a, b) => $"{a}&{b}")}";
    }

    public static SubscriptionType GetSubscriptionType(this HttpContext context, string? channelId)
    {
        if (channelId is null) return SubscriptionType.NONE;
        DatabaseUser? user = DatabaseManager.Users.GetUserFromToken(context.Request.Cookies["token"] ?? "").Result;
        if (user is null) return SubscriptionType.NONE;
        return user.Subscriptions.TryGetValue(channelId, out SubscriptionType type) ? type : SubscriptionType.NONE;
    }

    public static string GetExtension(this Format format)
    {
        if (format.MimeType.StartsWith("video"))
            format.MimeType
                .Split("/").Last()
                .Split(";").First();
        else
        {
            if (format.MimeType.Contains("opus"))
                return "opus";
            if (format.MimeType.Contains("mp4a"))
                return "mp3";
        }

        return "mp4";
    }

    public static string[] FindInvalidScopes(string[] scopes) =>
        scopes.Where(x => !OauthScopes.Contains(x)).ToArray();

    public static IEnumerable<string> GetScopeDescriptions(string[] modelScopes, LocalizationManager localization)
    {
        List<string> descriptions = [];

        // dangerous ones are at the top
        if (modelScopes.Contains("logins.read"))
            descriptions.Add("!" + localization.GetRawString("oauth2.scope.logins.read"));
        if (modelScopes.Contains("logins.delete"))
            descriptions.Add("!" + localization.GetRawString("oauth2.scope.logins.write"));

        descriptions.Add("Access YouTube data");

        if (modelScopes.Contains("playlists.read") && modelScopes.Contains("playlists.write"))
            descriptions.Add(localization.GetRawString("oauth2.scope.playlists.rw"));
        else if (modelScopes.Contains("playlists.read"))
            descriptions.Add(localization.GetRawString("oauth2.scope.playlists.read"));
        else if (modelScopes.Contains("playlists.write"))
            descriptions.Add(localization.GetRawString("oauth2.scope.playlists.write"));

        if (modelScopes.Contains("subscriptions.read"))
        {
            descriptions.Add(localization.GetRawString("oauth2.scope.subscriptions.read"));
            descriptions.Add(localization.GetRawString("oauth2.scope.subscriptions.feed"));
        }

        if (modelScopes.Contains("subscriptions.write"))
            descriptions.Add(localization.GetRawString("oauth2.scope.subscriptions.write"));

        return descriptions;
    }

    public static string GenerateToken(int length)
    {
        string tokenAlphabet = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        Random rng = new();
        StringBuilder sb = new();
        for (int i = 0; i < length; i++)
            sb.Append(tokenAlphabet[rng.Next(0, tokenAlphabet.Length)]);
        return sb.ToString();
    }

    public static SearchParams GetSearchParams(this HttpRequest request)
    {
        SearchParams searchParams = new()
        {
            Filters = new SearchFilters(),
            QueryFlags = new QueryFlags()
        };

        if (request.Query.TryGetValue("uploadDate", out StringValues uploadDateValues) &&
            int.TryParse(uploadDateValues, out int uploadDate))
            searchParams.Filters.UploadedIn = (SearchFilters.Types.UploadDate)uploadDate;

        if (request.Query.TryGetValue("type", out StringValues typeValues) && int.TryParse(typeValues, out int type))
            searchParams.Filters.Type = (SearchFilters.Types.ItemType)type;

        if (request.Query.TryGetValue("duration", out StringValues durationValues) &&
            int.TryParse(durationValues, out int duration))
            searchParams.Filters.Duration = (SearchFilters.Types.VideoDuration)duration;

        if (request.Query.TryGetValue("sortField", out StringValues sortFieldValues) &&
            int.TryParse(sortFieldValues, out int sortField))
            searchParams.SortBy = (SearchParams.Types.SortField)sortField;

        if (request.Query.TryGetValue("live", out StringValues _)) searchParams.Filters.Live = true;
        if (request.Query.TryGetValue("_4k", out StringValues _)) searchParams.Filters.Resolution4K = true;
        if (request.Query.TryGetValue("hd", out StringValues _)) searchParams.Filters.Hd = true;
        if (request.Query.TryGetValue("subs", out StringValues _)) searchParams.Filters.Subtitles = true;
        if (request.Query.TryGetValue("cc", out StringValues _)) searchParams.Filters.CreativeCommons = true;
        if (request.Query.TryGetValue("vr360", out StringValues _)) searchParams.Filters.Vr360 = true;
        if (request.Query.TryGetValue("vr180", out StringValues _)) searchParams.Filters.Vr180 = true;
        if (request.Query.TryGetValue("_3d", out StringValues _)) searchParams.Filters.Resolution3D = true;
        if (request.Query.TryGetValue("hdr", out StringValues _)) searchParams.Filters.Hdr = true;
        if (request.Query.TryGetValue("location", out StringValues _)) searchParams.Filters.Location = true;
        if (request.Query.TryGetValue("purchased", out StringValues _)) searchParams.Filters.Purchased = true;
        if (request.Query.TryGetValue("exact", out StringValues _)) searchParams.QueryFlags.ExactSearch = true;

        return searchParams;
    }

    public static bool ShouldShowAlert(HttpRequest request)
    {
        if (Configuration.AlertHash == null) return false;

        if (request.Cookies.TryGetValue("dismissedAlert", out string? cookieVal))
            return cookieVal != Configuration.AlertHash;

        return true;
    }

    public static string Md5Sum(string input)
    {
        using MD5 md5 = MD5.Create();
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes);
    }

    public static float ExtractHeaderQualityValue(string s)
    {
        // https://developer.mozilla.org/en-US/docs/Glossary/Quality_values
        string[] parts = s.Split("q=");
        return parts.Length > 1 && float.TryParse(parts[1], out float val) ? val : 1;
    }
}