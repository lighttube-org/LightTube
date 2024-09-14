namespace LightTube.PoToken;

public class PoTokenResponse
{
	public bool Success { get; set; }
	public PoTokenData? Response { get; set; }
	public string? Error { get; set; }
}