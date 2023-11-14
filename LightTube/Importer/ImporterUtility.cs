using System.IO.Compression;
using System.Text;
using LightTube.Database.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LightTube;

public static class ImporterUtility
{
	private static readonly string TakeoutMagicBytes = "504B03";

	public static ImportSource AutodetectSource(byte[] data)
	{
		if (data[0] == '{')
		{
			JObject obj = JsonConvert.DeserializeObject<JObject>(Encoding.UTF8.GetString(data))!;
			if (obj.ContainsKey("preferences")) return ImportSource.InvidiousSubscriptionManagerJson;
			if (obj.ContainsKey("format")) return ImportSource.PipedPlaylists;
			if (obj.ContainsKey("app_version") && obj.ContainsKey("subscriptions"))
				return ImportSource.PipedSubscriptions;
		}

		if (string.Join("", data.Take(3).Select(x => x.ToString("X2"))) == TakeoutMagicBytes)
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
				ImportedData.Subscription? item = new ImportedData.Subscription();
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
	PipedPlaylists
}