using System.Text.Json;
using System.Web;

namespace LightTube;

public class SearchAutocomplete(string query, string[] autocomplete)
{
	private static HttpClient client = new();
	public string Query { get; } = query;
	public string[] Autocomplete { get; } = autocomplete;

	public static async Task<SearchAutocomplete> GetAsync(string query, string language = "en", string region = "us")
	{
		string url = "https://suggestqueries-clients6.youtube.com/complete/search?client=firefox&ds=yt" +
		             $"&hl={HttpUtility.UrlEncode(language.ToLower())}" +
		             $"&gl={HttpUtility.UrlEncode(region.ToLower())}" +
		             $"&q={HttpUtility.UrlEncode(query)}";
		string json = await client.GetStringAsync(url);
		object[] parsed = JsonSerializer.Deserialize<object[]>(json)!;
		return new SearchAutocomplete(parsed[0] as string ?? query, parsed[1] as string[] ?? []);
	}
}