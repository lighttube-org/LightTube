using Newtonsoft.Json;

namespace LightTube;

public class SponsorBlockSegment
{
	[JsonProperty("category")] public string Category { get; set; }
	[JsonProperty("actionType")] public string ActionType { get; set; }
	[JsonProperty("segment")] private double[] Segment { get; set; }
	[JsonProperty("UUID")] public string Uuid { get; set; }
	[JsonProperty("videoDuration")] public double VideoDuration { get; set; }
	[JsonProperty("locked")] public long Locked { get; set; }
	[JsonProperty("votes")] public long Votes { get; set; }
	[JsonProperty("description")] public string Description { get; set; }
	public double StartMs => Segment[0];
	public double EndMs => Segment[1];

	public string ToLTPlayerJson(double videoDuration) => $"{{ from: {ToPercentage(StartMs, videoDuration)}, to: {ToPercentage(EndMs, videoDuration)}, color: '#ff0', onEnter: function(player) {{ player.showSkipButton('Skip {Category}', {EndMs});}},onExit:function(player) {{player.hideSkipButton();}} }}";

	private double ToPercentage(double input, double max) => (input / max) * 100;

	public static async Task<SponsorBlockSegment[]> GetSponsors(string videoId)
	{
		HttpResponseMessage sbResponse =
			await new HttpClient().GetAsync($"https://sponsor.ajay.app/api/skipSegments?videoID={videoId}&category=sponsor&category=selfpromo&category=interaction&category=intro&category=outro&category=preview&category=music_offtopic&category=filler");
		if (!sbResponse.IsSuccessStatusCode) return Array.Empty<SponsorBlockSegment>();
		string json = await sbResponse.Content.ReadAsStringAsync();
		return JsonConvert.DeserializeObject<SponsorBlockSegment[]>(json)!;
	}
}