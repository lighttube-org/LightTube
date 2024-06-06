using System.Xml.Linq;

namespace LightTube;

public static class YoutubeRss
{
    private static HttpClient httpClient = new();

    public static async Task<ChannelFeed> GetChannelFeed(string channelId)
    {
        HttpResponseMessage response =
            await httpClient.GetAsync("https://www.youtube.com/feeds/videos.xml?channel_id=" + channelId);
        if (!response.IsSuccessStatusCode)
            return new ChannelFeed(
                $"Failed to get channel videos: HTTP {(int)response.StatusCode}",
                channelId,
                []
            );

        string xml = await response.Content.ReadAsStringAsync();
        XDocument doc = XDocument.Parse(xml);

        ChannelFeed feed = new(
            doc.Descendants().First(p => p.Name.LocalName == "title").Value,
            doc.Descendants().First(p => p.Name.LocalName == "channelId").Value, doc
                .Descendants()
                .Where(p => p.Name.LocalName == "entry")
                .Select(x => new FeedVideo(
                    x.Descendants().First(p => p.Name.LocalName == "videoId").Value,
                    x.Descendants().First(p => p.Name.LocalName == "title").Value,
                    x.Descendants().First(p => p.Name.LocalName == "description").Value,
                    long.Parse(x.Descendants().First(p => p.Name.LocalName == "statistics").Attribute("views")?.Value ??
                               "-1"),
                    x.Descendants().First(p => p.Name.LocalName == "thumbnail").Attribute("url")?.Value ??
                    $"https://i.ytimg.com/vi/{x.Descendants().First(p => p.Name.LocalName == "videoId").Value}/hqdefault.jpg",
                    x.Descendants().First(p => p.Name.LocalName == "name").Value,
                    x.Descendants().First(p => p.Name.LocalName == "channelId").Value,
                    DateTimeOffset.Parse(x.Descendants().First(p => p.Name.LocalName == "published").Value)
                ))
        );

        return feed;
    }

    public static async Task<FeedVideo[]> GetMultipleFeeds(IEnumerable<string> channelIds)
    {
        Task<ChannelFeed>[] feeds = channelIds.Select(GetChannelFeed).ToArray();
        await Task.WhenAll(feeds);

        List<FeedVideo> videos = [];
        foreach (ChannelFeed feed in feeds.Select(x => x.Result)) videos.AddRange(feed.Videos);

        videos.Sort((a, b) => DateTimeOffset.Compare(b.PublishedDate, a.PublishedDate));
        return videos.ToArray();
    }
}

public class ChannelFeed(string id, string name, IEnumerable<FeedVideo> videos)
{
    public string Name = name;
    public string Id = id;
    public FeedVideo[] Videos = videos.ToArray();
}

public class FeedVideo(
    string id,
    string title,
    string description,
    long viewCount,
    string thumbnail,
    string channelName,
    string channelId,
    DateTimeOffset publishedDate)
{
    public string Id = id;
    public string Title = title;
    public string Description = description;
    public long ViewCount = viewCount;
    public string Thumbnail = thumbnail;
    public string ChannelName = channelName;
    public string ChannelId = channelId;
    public DateTimeOffset PublishedDate = publishedDate;
}