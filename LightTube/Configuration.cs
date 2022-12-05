using InnerTube;

namespace LightTube;

public static class Configuration
{
	private static Dictionary<string, string> _variables = new();

	public static string? GetVariable(string var, string? def = null)
	{
		if (_variables.TryGetValue(var, out string? res)) return res;
		string? v = Environment.GetEnvironmentVariable(var) ?? def;
		if (v == null) return null;
		_variables.Add(var, v);
		return v;
	}
	
	public static InnerTubeAuthorization? GetInnerTubeAuthorization() =>
		GetVariable("LIGHTTUBE_AUTH_TYPE")?.ToLower() switch
		{
			"cookie" => InnerTubeAuthorization.SapisidAuthorization(
				GetVariable("LIGHTTUBE_AUTH_SAPISID") ??
				throw new ArgumentNullException("LIGHTTUBE_AUTH_SAPISID",
					"Authentication type set to 'cookie' but the 'LIGHTTUBE_AUTH_SAPISID' environment variable is not set."),
				GetVariable("LIGHTTUBE_AUTH_PSID") ??
				throw new ArgumentNullException("LIGHTTUBE_AUTH_PSID",
					"Authentication type set to 'cookie' but the 'LIGHTTUBE_AUTH_PSID' environment variable is not set.")),
			"oauth2" => InnerTubeAuthorization.RefreshTokenAuthorization(
				GetVariable("LIGHTTUBE_AUTH_REFRESH_TOKEN") ??
				throw new ArgumentNullException("LIGHTTUBE_AUTH_REFRESH_TOKEN",
					"Authentication type set to 'oauth2' but the 'LIGHTTUBE_AUTH_REFRESH_TOKEN' environment variable is not set.")),
			var _ => null
		};
}