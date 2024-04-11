using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Html;
using Serilog;

namespace LightTube.Localization;

public class LocalizationManager(string locale)
{
	private static Dictionary<string, Dictionary<string, string>> Localizations { get; set; } = [];
	private Dictionary<string, string> PreferredLocaleStrings { get; set; }
	private Dictionary<string, string> FallbackLocaleStrings { get; set; }

	public string CurrentLocale => locale;

	public string GetRawString(string key, bool forceFallback = false)
	{
		if (!forceFallback && PreferredLocaleStrings.TryGetValue(key, out string? preferred))
			return preferred;

		if (FallbackLocaleStrings.TryGetValue(key, out string? fallback))
			return fallback;

		return key;
	}

	public HtmlString GetString(string key, bool forceFallback = false) => new(GetRawString(key, forceFallback));

	public HtmlString FormatString(string key, params object[] args)
	{
		string res;
		string?[] formatArgs = args.Select(HttpUtility.HtmlEncode).ToArray();
		try
		{
			res = string.Format(GetRawString(key), args: formatArgs);
		}
		catch (FormatException)
		{
			try
			{
				res = string.Format(GetRawString(key, true), args: formatArgs);
			}
			catch (FormatException)
			{
				res = $"{key}:{string.Join(':', formatArgs)}";
			}
		}

		return new HtmlString(res);
	}


	public static void Init()
	{
		Log.Information("[Localization] Initializing localization files...");
		foreach (string file in Directory.GetFiles("Resources/Localization"))
		{
			string code = file.Split('/')[2].Split('.')[0];
			using FileStream s = File.Open(file, FileMode.Open, FileAccess.Read);
			Localizations.Add(code, JsonSerializer.Deserialize<Dictionary<string, string>>(s) ?? []);
			s.Close();
			Log.Information("[Localization] Loaded localization file for {0} with {1} keys", code,
				Localizations[code].Count);
		}

		Log.Information("[Localization] Localization files initialized.");
	}

	public static LocalizationManager GetFromHttpContext(HttpContext context)
	{
		string preferredLocale = "en_US";

		string acceptLanguageHeaderValue = context.Request.Headers.AcceptLanguage.FirstOrDefault() ?? "en-US";
		Dictionary<string, string> preferredLocaleStrings = [];

		foreach (string language in acceptLanguageHeaderValue.Split(',')
			         .Select(x => x.Trim())
			         .Where(x => !string.IsNullOrEmpty(x))
			         .OrderByDescending(Utils.ExtractHeaderQualityValue))
		{
			string code = language.Split(';')[0];
			if (!Localizations.TryGetValue(code, out Dictionary<string, string>? preferredStr)) continue;

			preferredLocale = code;
			preferredLocaleStrings = preferredStr;
			break;
		}

		return new LocalizationManager(preferredLocale)
		{
			PreferredLocaleStrings = preferredLocaleStrings,
			FallbackLocaleStrings = Localizations["en-US"]
		};
	}
}