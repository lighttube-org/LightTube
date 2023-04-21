using LightTube.Database.Models;
using Newtonsoft.Json;

namespace LightTube.ApiModels;

public class Oauth2CodeGrantResponse
{
	[JsonProperty("access_token")] public string AccessToken;
	[JsonProperty("token_type")] public string TokenType;
	[JsonProperty("expires_in")] public int ExpiresIn;
	[JsonProperty("refresh_token")] public string RefreshToken;
	[JsonProperty("scope")] public string Scope;

	public Oauth2CodeGrantResponse(DatabaseOauthToken token)
	{
		AccessToken = token.CurrentAuthToken;
		TokenType = "Bearer";
		ExpiresIn = (int)Math.Round(token.CurrentTokenExpirationDate.Subtract(DateTimeOffset.Now).TotalSeconds);
		RefreshToken = token.RefreshToken;
		Scope = string.Join(" ", token.Scopes);
	}
}