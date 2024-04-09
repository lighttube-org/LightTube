using InnerTube;

namespace LightTube.Controllers;

public class ModifyPlaylistContentResponse(InnerTubePlayer video)
{
    public string Title = video.Details.Title;
    public string Author = video.Details.Author.Title;
    public string Thumbnail = $"https://i.ytimg.com/vi/{video.Details.Id}/hqdefault.jpg";
}