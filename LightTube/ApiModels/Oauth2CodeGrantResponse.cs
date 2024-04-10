using LightTube.Database.Models;
using Newtonsoft.Json;

namespace LightTube.ApiModels;

public class Oauth2CodeGrantResponse(DatabaseOauthToken token)
{
    [JsonProperty("access_token")] public string AccessToken = token.CurrentAuthToken;
    [JsonProperty("token_type")] public string TokenType = "Bearer";
    [JsonProperty("expires_in")] public int ExpiresIn = (int)Math.Round(token.CurrentTokenExpirationDate.Subtract(DateTimeOffset.Now).TotalSeconds);
    [JsonProperty("refresh_token")] public string RefreshToken = token.RefreshToken;
    [JsonProperty("scope")] public string Scope = string.Join(" ", token.Scopes);
}