using LightTube.Localization;

namespace LightTube.Contexts;

public class AccountContext(HttpContext context)
{
    public string? HtmlTitle { get; set; }
    public string? Redirect { get; set; }
    public string? Error { get; set; }
    public string? UserID { get; set; }
    public LocalizationManager Localization { get; set; } = LocalizationManager.GetFromHttpContext(context);
}