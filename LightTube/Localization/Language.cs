using System.Globalization;

namespace LightTube.Localization;

public class Language
{
	public string Code { get; set; }
	public string Name { get; set; }
	public string EnglishName { get; set; }
	public CultureInfo Culture { get; set; }

	public string GetDisplayName() => Name == EnglishName ? Name : $"{Name} - {EnglishName}";
}