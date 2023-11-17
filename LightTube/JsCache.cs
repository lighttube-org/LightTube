using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace LightTube;

public static class JsCache
{
	private static Dictionary<string, Uri> LibraryUrls = new()
	{
		["hls.js"] = new Uri("https://cdn.jsdelivr.net/npm/hls.js@1.4.0/dist/hls.min.js"),
		["ltplayer.js"] = new Uri("https://raw.githubusercontent.com/kuylar/LTPlayer/master/dist/player.min.js"),
		["ltplayer.css"] = new Uri("https://raw.githubusercontent.com/kuylar/LTPlayer/master/dist/player.min.css"),
	};
	private static Dictionary<string, string> Hashes = new();
	public static DateTimeOffset CacheUpdateTime = DateTimeOffset.MinValue;

	public static async Task DownloadLibraries()
	{
		HttpClient client = new();
		Directory.CreateDirectory("/tmp/lighttube/jsCache");
		Console.WriteLine("[JsCache] Downloading libraries...");
		foreach ((string? name, Uri? url) in LibraryUrls)
		{
			Console.WriteLine($"[JsCache] Downloading '{name}' from {url}");

			HttpResponseMessage response = await client.GetAsync(url);
			string jsData = await response.Content.ReadAsStringAsync();
			await File.WriteAllTextAsync($"/tmp/lighttube/jsCache/{name}", jsData);
			Console.WriteLine($"[JsCache] Calculating the MD5 hash of {name}...");
			
			using MD5 md5 = MD5.Create();
			byte[] inputBytes = Encoding.ASCII.GetBytes(jsData);
			byte[] hashBytes = md5.ComputeHash(inputBytes);
			string hash = Convert.ToHexString(hashBytes);

			Hashes[name] = hash;
			
			Console.WriteLine($"[JsCache] Downloaded '{name}'.");
		}
		CacheUpdateTime = DateTimeOffset.UtcNow;
	}

	public static string GetJsFileContents(string name) => File.ReadAllText($"/tmp/lighttube/jsCache/{HttpUtility.UrlEncode(name)}");
	public static Uri GetUrl(string name) => LibraryUrls.TryGetValue(name, out Uri? url) ? url : new Uri("/");

	public static string GetHash(string name) =>
		Hashes.TryGetValue(name, out string? h) ? h : "68b329da9893e34099c7d8ad5cb9c940"; // md5 sum of an empty buffer
}