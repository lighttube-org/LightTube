using InnerTube;

namespace LightTube.Contexts;

public class AppearanceSettingsContext(HttpContext context, InnerTubeLocals locals, Dictionary<string, string> customThemes) : BaseContext(context)
{
    public InnerTubeLocals Locals = locals;
    public Dictionary<string, string> CustomThemes = customThemes;
    public Dictionary<string, string> BuiltinThemes = new()
    {
        ["auto"] = "System Default",
        ["light"] = "Light",
        ["dark"] = "Dark",
    };
}