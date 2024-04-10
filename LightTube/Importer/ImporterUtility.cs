using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using LightTube.Database.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LightTube;

public static class ImporterUtility
{
    private static readonly byte[] TakeoutMagicBytes = [0x50, 0x4B, 0x03];

    private static ImportSource AutodetectSource(byte[] data)
    {
        if (data.Length == 0) return ImportSource.Unknown;

        if (data[0] == '{')
        {
            JObject obj = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(data))!;
            if (obj.ContainsKey("preferences")) return ImportSource.InvidiousSubscriptionManagerJson;
            if (obj.ContainsKey("format")) return ImportSource.PipedPlaylists;
            if (obj.ContainsKey("app_version") && obj.ContainsKey("subscriptions"))
                return ImportSource.PipedSubscriptions;
            if (obj["type"]?.ToObject<string>()?.StartsWith("LightTube/") ?? false)
                return ImportSource.LightTubeExport;
        }

        if (data[0] == TakeoutMagicBytes[0] && data[1] == TakeoutMagicBytes[1] && data[2] == TakeoutMagicBytes[2])
            return ImportSource.YoutubeTakeoutZip;

        if (Encoding.UTF8.GetString(data.Take(5).ToArray()) == "<opml")
            return ImportSource.InvidiousSubscriptionManagerXml;

        return ImportSource.Unknown;
    }


    public static ImportedData ExtractData(byte[] data)
    {
        ImportSource src = AutodetectSource(data);
        return src switch
        {
            ImportSource.YoutubeTakeoutZip => ExtractTakeoutZip(data),
            ImportSource.InvidiousSubscriptionManagerXml => ExtractInvidiousOpml(data),
            ImportSource.InvidiousSubscriptionManagerJson => ExtractInvidiousJson(data),
            ImportSource.PipedSubscriptions => ExtractPipedSubscriptions(data),
            ImportSource.PipedPlaylists => ExtractPipedPlaylists(data),
            ImportSource.LightTubeExport => ExtractLightTube(data),
            ImportSource.Unknown => throw new NotSupportedException("Could not detect file type"),
            _ => throw new NotSupportedException($"Export type {src.ToString()} is not implemented")
        };
    }

    private static ImportedData ExtractTakeoutZip(byte[] data)
    {
        ImportedData importedData = new(ImportSource.YoutubeTakeoutZip);
        using MemoryStream zipStream = new(data);
        using ZipArchive archive = new(zipStream);
        List<string> fileTree = archive.Entries.Select(entry => entry.FullName).ToList();
        ZipArchiveEntry?[] playlistFiles = fileTree
            .Where(x => x.Contains("/playlists/"))
            .Select(x => archive.GetEntry(x))
            .Where(x => x != null)
            .ToArray();
        ZipArchiveEntry? subscriptionsFile = archive.GetEntry(fileTree
            .First(x => x.Contains("/subscriptions/subscriptions.csv")));

        if (subscriptionsFile != null)
        {
            using Stream subsStream = subscriptionsFile.Open();
            using StreamReader subsReader = new(subsStream);

            string csv = subsReader.ReadToEnd().Trim();
            string[] lines = csv.Split('\n').Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            foreach (string line in lines)
            {
                string[] parts = line.Split(',');
                ImportedData.Subscription? item = new();
                item.Id = parts[0];
                item.Name = parts[2];
                importedData.Subscriptions.Add(item);
            }

            subsReader.Close();
            subsStream.Close();
        }

        foreach (ZipArchiveEntry? entry in playlistFiles)
        {
            if (entry is null) continue;
            using Stream playlistStream = entry.Open();
            using StreamReader listReader = new(playlistStream);

            string csv = listReader.ReadToEnd().Trim();

            string[] csvParts = csv.Split("\n\n\n\n\n\n");
            string[] infoPart = csvParts[0].Trim().Split('\n');
            string[] infoParts = infoPart[1].Split(',');
            string[] videosPart = csvParts[1].Trim().Split('\n').Skip(1).ToArray();

            // Uploads from <user> playlist has no creation date, and its kinda unnecessary to import
            if (string.IsNullOrEmpty(infoParts[2])) continue;
            ImportedData.Playlist? item = new();
            item.Title = infoParts[4];
            item.Description = infoParts[5];
            item.TimeCreated = DateTimeOffset.Parse(infoParts[2]);
            item.TimeUpdated = DateTimeOffset.Parse(infoParts[3]);
            item.Visibility = infoParts[6] switch
            {
                "Public" => PlaylistVisibility.VISIBLE,
                "Unlisted" => PlaylistVisibility.UNLISTED,
                "Private" => PlaylistVisibility.PRIVATE,
                _ => PlaylistVisibility.PRIVATE
            };
            item.VideoIds = videosPart.Select(x => x.Split(',')[0]).ToArray();
            importedData.Playlists.Add(item);

            listReader.Close();
            playlistStream.Close();
        }

        archive.Dispose();
        zipStream.Dispose();
        return importedData;
    }

    private static ImportedData ExtractInvidiousOpml(byte[] data)
    {
        ImportedData importedData = new(ImportSource.InvidiousSubscriptionManagerXml);
        string xmlText = Encoding.UTF8.GetString(data);
        XDocument xml = XDocument.Parse(xmlText);
        XElement container =
            xml.Descendants().First(x => x.Attribute("text")?.Value.Contains("Subscriptions") ?? false);
        foreach (XElement el in container.Descendants())
        {
            string title = el.Attribute("title")!.Value;
            Uri url = new(el.Attribute("xmlUrl")!.Value);
            string id = url.Host.EndsWith("youtube.com") ? url.Query.Split("=")[1] : url.AbsolutePath.Split("/").Last();
            importedData.Subscriptions.Add(new ImportedData.Subscription
            {
                Id = id,
                Name = title
            });
        }

        return importedData;
    }

    private static ImportedData ExtractInvidiousJson(byte[] data)
    {
        ImportedData importedData = new(ImportSource.InvidiousSubscriptionManagerJson);
        string json = Encoding.UTF8.GetString(data);
        JObject obj = JObject.Parse(json);

        foreach (JToken jToken in obj["subscriptions"]?.ToObject<JArray>() ?? [])
        {
            if (jToken.Type != JTokenType.String) continue;
            string id = jToken.ToObject<string>()!;
            importedData.Subscriptions.Add(new ImportedData.Subscription
            {
                Id = id
            });
        }

        foreach (JToken playlist in obj["playlists"]?.ToObject<JArray>() ?? [])
        {
            if (playlist.Type != JTokenType.Object) continue;
            importedData.Playlists.Add(new ImportedData.Playlist
            {
                Title = playlist["title"]!.ToObject<string>()!,
                Description = playlist["description"]!.ToObject<string>()!,
                TimeCreated = null,
                TimeUpdated = null,
                Visibility = playlist["privacy"]!.ToObject<string>()! switch
                {
                    "Public" => PlaylistVisibility.VISIBLE,
                    "Unlisted" => PlaylistVisibility.UNLISTED,
                    "Private" => PlaylistVisibility.PRIVATE,
                    _ => PlaylistVisibility.PRIVATE
                },
                VideoIds = playlist["videos"]!.ToObject<string[]>()!
            });
        }

        return importedData;
    }

    private static ImportedData ExtractPipedSubscriptions(byte[] data)
    {
        ImportedData importedData = new(ImportSource.PipedSubscriptions);
        string json = Encoding.UTF8.GetString(data);
        JObject obj = JObject.Parse(json);

        foreach (JToken subscription in obj["subscriptions"]?.ToObject<JArray>() ?? [])
        {
            if (subscription["service_id"]?.ToObject<int>() != 0) continue;

            importedData.Subscriptions.Add(new ImportedData.Subscription
            {
                Id = subscription["url"]!.ToObject<string>()!.Split("/")[4],
                Name = subscription["name"]?.ToObject<string>()
            });
        }

        return importedData;
    }

    private static ImportedData ExtractPipedPlaylists(byte[] data)
    {
        ImportedData importedData = new(ImportSource.PipedPlaylists);
        string json = Encoding.UTF8.GetString(data);
        JObject obj = JObject.Parse(json);

        foreach (JToken playlist in obj["playlists"]?.ToObject<JArray>() ?? [])
        {
            // Piped seems to (plan to) support other types of playlists
            // (watch later, history, etc.)
            // https://github.com/TeamPiped/Piped/blob/1c74bd1196172e52da7f23fcbf08ba68bc3cd911/src/components/PlaylistsPage.vue#L179
            if (playlist["type"]?.ToObject<string>() != "playlist") continue;

            importedData.Playlists.Add(new ImportedData.Playlist
            {
                Title = playlist["name"]!.ToObject<string>()!,
                Description = "", // Piped doesn't export playlist descriptions
                TimeCreated = null,
                TimeUpdated = null,
                // Piped doesn't seem to have playlist privacy, and
                // from my testing, I could just access a playlist I
                // created without logging in (which makes it not private)
                Visibility = PlaylistVisibility.UNLISTED,
                VideoIds = playlist["videos"]!.ToObject<string[]>()!.Select(x => x.Split("?v=")[1]).ToArray()
            });
        }

        return importedData;
    }

    private static ImportedData ExtractLightTube(byte[] data)
    {
        ImportedData importedData = new(ImportSource.LightTubeExport);
        string json = Encoding.UTF8.GetString(data);
        LightTubeExport obj = JsonConvert.DeserializeObject<LightTubeExport>(json)!;

        importedData.Subscriptions.AddRange(obj.Subscriptions
            .Select(x => new ImportedData.Subscription { Id = x, Name = null }));
        importedData.Playlists = [.. obj.Playlists];

        return importedData;
    }
}

public enum ImportSource
{
    Unknown,
    YoutubeTakeoutZip,
    InvidiousSubscriptionManagerJson,
    InvidiousSubscriptionManagerXml,
    PipedSubscriptions,
    PipedPlaylists,
    LightTubeExport
}