using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Primitives;

namespace LightTube;

public static class Utils
{
	private static string? _version;

	public static string GetRegion(this HttpContext context) =>
		context.Request.Headers.TryGetValue("X-Content-Region", out StringValues h) ? h.ToString() :
		context.Request.Cookies.TryGetValue("gl", out string region) ? region : "US";

	public static string GetLanguage(this HttpContext context) =>
		context.Request.Headers.TryGetValue("X-Content-Language", out StringValues h) ? h.ToString() :
		context.Request.Cookies.TryGetValue("hl", out string language) ? language : "en";

	public static string GetVersion()
	{
		if (_version is null)
		{
#if DEBUG
			DateTime buildTime = DateTime.Today;
			_version = $"{buildTime.Year}.{buildTime.Month}.{buildTime.Day}";
#else
				_version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location)
					.FileVersion?[2..];
#endif
			if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
				_version += " (dev)";
		}
		return _version;
	}
}