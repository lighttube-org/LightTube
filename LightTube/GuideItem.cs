namespace LightTube;

public class GuideItem(string title, string iconId, string path, bool visibleOnMinifiedGuide)
{
	public string Title = title;
	public string IconId = iconId;
	public string Path = path;
	public bool VisibleOnMinifiedGuide = visibleOnMinifiedGuide;
}