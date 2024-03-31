namespace LightTube;

public class DropdownItem
{
	public string Label;
	public string Target;
	public string Icon;

	public DropdownItem(string label, string target, string icon)
	{
		Label = label;
		Target = target;
		Icon = icon;
	}
}