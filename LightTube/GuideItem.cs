namespace LightTube;

public class GuideItem
{
	public string Title;
	public string IconId;
	public string Path;
	public bool VisibleOnMinifiedGuide;

	public GuideItem(string title, string iconId, string path, bool visibleOnMinifiedGuide)
	{
		Title = title;
		IconId = iconId;
		Path = path;
		VisibleOnMinifiedGuide = visibleOnMinifiedGuide;
	}
}