using System.Collections.Generic;
using InnerTube.Models;

namespace LightTube.Contexts
{
	public class SettingsContext : BaseContext
	{
		public Dictionary<string, string> Languages;
		public Dictionary<string, string> Regions;
		public string CurrentLanguage;
		public string CurrentRegion;
		public string Theme;
		public bool CompatibilityMode;
		public bool ApiAccess;
	}
}