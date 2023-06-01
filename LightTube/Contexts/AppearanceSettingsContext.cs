using InnerTube;

namespace LightTube.Contexts;

public class AppearanceSettingsContext : BaseContext
{
	public InnerTubeLocals Locals;
	public Dictionary<string, string> CustomThemes;
	public Dictionary<string, string> BuiltinThemes = new()
	{
		["auto"] = "System Default",
		["light"] = "Light",
		["dark"] = "Dark",
	};

	public AppearanceSettingsContext(HttpContext context, InnerTubeLocals locals, Dictionary<string, string> customThemes) : base(context)
	{
		Locals = locals;
		CustomThemes = customThemes;
	}
}