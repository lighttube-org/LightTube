using System.Text;
using LightTube.Database.Models;

namespace LightTube;

public class ImportedData
{
	public ImportSource Source;
	public List<Subscription> Subscriptions;
	public List<Playlist> Playlists;

	public ImportedData(ImportSource source)
	{
		Source = source;
		Subscriptions = new List<Subscription>();
		Playlists = new List<Playlist>();
	}

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
				.AppendLine("TimeCreated: " + (p.TimeCreated.ToString() ?? "<not provided>"))
				.AppendLine("TimeUpdated: " + (p.TimeUpdated.ToString() ?? "<not provided>"))
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
		public string Title;
		public string Description;
		public DateTimeOffset? TimeCreated;
		public DateTimeOffset? TimeUpdated;
		public PlaylistVisibility Visibility;
		public string[] VideoIds;
	}
}