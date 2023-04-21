using InnerTube;

namespace LightTube.Controllers;

public class ModifyPlaylistContentResponse
{
	public string Title;
	public string Author;
	public string Thumbnail;

	public ModifyPlaylistContentResponse(InnerTubePlayer video)
	{
		Title = video.Details.Title;
		Author = video.Details.Author.Title;
		Thumbnail = $"https://i.ytimg.com/vi/{video.Details.Id}/hqdefault.jpg";
	}
}