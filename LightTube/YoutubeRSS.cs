using InnerTube;
using InnerTube.Renderers;

namespace LightTube;

public static class YoutubeRSS
{
	private static InnerTube.InnerTube _innerTube = new();

	/*
	 * BANDAID CODE AFTER YOUTUBE REMOVED THE RSS ENDPOINT
	 * PLEASE, *PLEASE* DONT USE THIS IN v3
	 *
	 * here have a nyan cat
	 *
	 * ⠀⠀⠀ ⠀⠀⠀⢀⣀⣀⣀⣤⣤⣤⣤⣤⣤⣤⣤⣤⣤⣤⣤⣤⣀⣀⠀⠀⠀⠀⠀
	 *  ⠀⠀⠀⠀⠀⠀⣾⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⠀⠀⠀⠀⢀⠀⠈⡇⠀⠀⠀⠀
	 *  ⠀⠀⠀⠀⠀⠀⣿⠀⠁⠀⠘⠁⠀⠀⠀⠀⠀⣀⡀⠀⠀⠀⠈⠀⠀⡇⠀⠀⠀⠀
	 *  ⣀⣀⣀⠀⠀⠀⣿⠀⠀⠀⠀⠀⠄⠀⠀⠸⢰⡏⠉⠳⣄⠰⠀⠀⢰⣷⠶⠛⣧⠀
	 *  ⢻⡀⠈⠙⠲⡄⣿⠀⠀⠀⠀⠀⠀⠀⠠⠀⢸⠀⠀⠀⠈⠓⠒⠒⠛⠁⠀⠀⣿⠀
	 *  ⠀⠻⣄⠀⠀⠙⣿⠀⠀⠀⠈⠁⠀⢠⠄⣰⠟⠀⢀⡔⢠⠀⠀⠀⠀⣠⠠⡄⠘⢧
	 *  ⠀⠀⠈⠛⢦⣀⣿⠀⠀⢠⡆⠀⠀⠈⠀⣯⠀⠀⠈⠛⠛⠀⠠⢦⠄⠙⠛⠃⠀⢸
	 *  ⠀⠀⠀⠀⠀⠉⣿⠀⠀⠀⢠⠀⠀⢠⠀⠹⣆⠀⠀⠀⠢⢤⠠⠞⠤⡠⠄⠀⢀⡾
	 *  ⠀⠀⠀⠀⠀⢀⡿⠦⢤⣤⣤⣤⣤⣤⣤⣤⡼⣷⠶⠤⢤⣤⣤⡤⢤⡤⠶⠖⠋⠀
	 *  ⠀⠀⠀⠀⠀⠸⣤⡴⠋⠸⣇⣠⠼⠁⠀⠀⠀⠹⣄⣠⠞⠀⢾⡀⣠⠃⠀⠀⠀⠀
	 *  ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠉⠁⠀⠀⠀⠀⠀
	 */
	public static async Task<ChannelFeed> GetChannelFeed(string channelId)
	{
		try
		{
			InnerTubeChannelResponse response = await _innerTube.GetChannelAsync(channelId, ChannelTabs.Videos);
			DateTimeOffset reference = new(DateTime.Today);
			List<FeedVideo> videos = new();

			foreach (IRenderer renderer in response.Contents.Select(x =>
				         x is RichItemRenderer rir ? rir.Content : x
			         ))
			{
				if (renderer is VideoRenderer video)
				{
					videos.Add(new FeedVideo
					{
						Id = video.Id,
						Title = video.Title,
						Description = video.Description,
						ViewCount = (long)InnerTube.Utils.ParseNumber(video.ViewCount),
						Thumbnail = video.Thumbnails.FirstOrDefault()?.Url.ToString() ?? "",
						ChannelName = response.Metadata.Title,
						ChannelId = response.Metadata.Id,
						PublishedDate = ParseTimeAgo(reference, video.Published ?? "1 year ago")
					});
				}
			}

			return new ChannelFeed
			{
				Name = response.Metadata.Title,
				Id = response.Metadata.Id,
				Videos = videos.ToArray()
			};
		}
		catch (Exception)
		{
			return new ChannelFeed
			{
				Name = "Failed to get videos for channel " + channelId,
				Id = channelId
			};
		}
	}

	public static DateTimeOffset ParseTimeAgo(DateTimeOffset reference, string time)
	{
		string[] parts = time.ToLower().Split(" ");
		int amount = int.Parse(parts[0]);
		return parts[1].TrimEnd('s') switch
		{
			"second" => reference.AddSeconds(-amount),
			"minute" => reference.AddMinutes(-amount),
			"hour" => reference.AddHours(-amount),
			"day" => reference.AddDays(-amount),
			"week" => reference.AddDays(-amount * 7),
			"month" => reference.AddMonths(-amount),
			"year" => reference.AddYears(-amount),
			_ => throw new KeyNotFoundException("Unknown timeago metric, " + parts[1].TrimEnd('s'))
		};
	}

	public static async Task<FeedVideo[]> GetMultipleFeeds(IEnumerable<string> channelIds)
	{
		Task<ChannelFeed>[] feeds = channelIds.Select(GetChannelFeed).ToArray();
		await Task.WhenAll(feeds);

		List<FeedVideo> videos = new();
		foreach (ChannelFeed feed in feeds.Select(x => x.Result)) videos.AddRange(feed.Videos);

		videos.Sort((a, b) => DateTimeOffset.Compare(b.PublishedDate, a.PublishedDate));
		return videos.ToArray();
	}
}

public class ChannelFeed
{
	public string Name;
	public string Id;
	public FeedVideo[] Videos;
}

public class FeedVideo
{
	public string Id;
	public string Title;
	public string Description;
	public long ViewCount;
	public string Thumbnail;
	public string ChannelName;
	public string ChannelId;
	public DateTimeOffset PublishedDate;
}