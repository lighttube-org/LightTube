using InnerTube;
using MongoDB.Bson.Serialization.Attributes;

namespace LightTube.Database.Models;

[BsonIgnoreExtraElements]
public class DatabaseVideo
{
	public string Id;
	public string Title;
	public Thumbnail[] Thumbnails;
	public string UploadedAt;
	public long Views;
	[BsonIgnore] public string ViewsCount => $"{Views:N0} views";
	public DatabaseVideoAuthor Channel;
	public string Duration;

	public DatabaseVideo()
	{
	}

	public DatabaseVideo(InnerTubePlayer player)
	{
		Id = player.Details.Id;
		Title = player.Details.Title;
		Thumbnails = new Thumbnail[] {
			new()
			{
				Url = new Uri($"https://i.ytimg.com/vi/{player.Details.Id}/hqdefault.jpg")
			}
		};
		UploadedAt = "";
		Views = 0;
		Channel = new()
		{
			Id = player.Details.Author.Id!,
			Name = player.Details.Author.Title,
			Avatars = new Thumbnail[] {
				new Thumbnail()
				{
					Url = player.Details.Author.Avatar!
				}
			}
		};
		Duration = player.Details.Length.ToDurationString();
	}
}