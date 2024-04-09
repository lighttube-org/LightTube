using System.Text;
using LightTube.Database.Models;
using Newtonsoft.Json;

namespace LightTube;

public class ImportedData(ImportSource source)
{
    public ImportSource Source = source;
    public List<Subscription> Subscriptions = [];
    public List<Playlist> Playlists = [];

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.AppendLine("=== EXPORTED DATA ===")
            .AppendLine("Source: " + Source)
            .AppendLine()
            .AppendLine("=== Channels");
        foreach (Subscription s in Subscriptions)
            sb.AppendLine(s.Id + ": " + (s.Name ?? "<not provided>"));

        sb.AppendLine()
            .AppendLine("=== Playlists");

        foreach (Playlist p in Playlists)
        {
            sb.AppendLine("Title: " + p.Title)
                .AppendLine("Description: " + p.Description)
                .AppendLine("TimeCreated: " + (p.TimeCreated?.ToString() ?? "<not provided>"))
                .AppendLine("TimeUpdated: " + (p.TimeUpdated?.ToString() ?? "<not provided>"))
                .AppendLine("Visibility: " + p.Visibility)
                .AppendLine(string.Join("\n", p.VideoIds.Select(x => $"- {x}")));
            sb.AppendLine("===");
        }

        return sb.ToString();
    }

    public class Subscription
    {
        public string Id;
        public string? Name;
    }

    public class Playlist
    {
        [JsonProperty("title")] public string Title;
        [JsonProperty("description")] public string Description;
        [JsonProperty("created")] public DateTimeOffset? TimeCreated;
        [JsonProperty("updated")] public DateTimeOffset? TimeUpdated;
        [JsonProperty("visibility")] public PlaylistVisibility Visibility;
        [JsonProperty("videos")] public string[] VideoIds;
    }
}