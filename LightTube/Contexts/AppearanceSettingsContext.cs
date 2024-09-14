using InnerTube;
using LightTube.ApiModels;
using LightTube.Localization;

namespace LightTube.Contexts;

public class AppearanceSettingsContext(
    HttpContext context,
    ApiLocals locals,
    Dictionary<string, string> customThemes,
    Language[] languages,
    Dictionary<string, int> languagePercentages) : BaseContext(context)
{
    public Language[] Languages = languages;
    public Dictionary<string, int> LanguagePercentages { get; set; } = languagePercentages;
    public ApiLocals Locals = locals;
    public Dictionary<string, string> CustomThemes = customThemes;
    public Dictionary<string, string> BuiltinThemes = new()
    {
        ["auto"] = "System Default",
        ["light"] = "Light",
        ["dark"] = "Dark",
    };

}