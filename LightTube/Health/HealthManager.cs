namespace LightTube.Health;

public static class HealthManager
{
	private static List<KeyValuePair<string, bool>> videoStatuses = [];
	private static List<long> playerResponseTimes = [];

	public static void PushVideoResponse(string videoId, bool isSuccess, long playerResponseTime)
	{
		if (videoStatuses.Count >= 100) videoStatuses.RemoveAt(0);
		if (playerResponseTimes.Count >= 100) playerResponseTimes.RemoveAt(0);
		
		playerResponseTimes.Add(playerResponseTime);

		KeyValuePair<string, bool>? oldData = videoStatuses.FirstOrDefault(x => x.Key == videoId);
		if (oldData?.Key == null) oldData = null; // unsure why the entire thing isnt null

		if (oldData?.Value == isSuccess) return;
		videoStatuses.Add(new KeyValuePair<string, bool>(videoId, isSuccess));
	}

	public static float GetHealthPercentage() =>
		Math.Clamp(MathF.Round((float)videoStatuses.Count(x => x.Value) / Math.Max(videoStatuses.Count, 1) * 100), 0, 100);

	public static double GetAveragePlayerResponseTime()
	{
		if (playerResponseTimes.Count == 0) return 0;
		 return playerResponseTimes.Average();
	}
}