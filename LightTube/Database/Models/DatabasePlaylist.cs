using InnerTube.Models;
using InnerTube.Protobuf;
using LightTube.Localization;
using Endpoint = InnerTube.Protobuf.Endpoint;

namespace LightTube.Database.Models;

public class DatabasePlaylist
{
	private const string ID_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
	public string Id;
	public string Name;
	public string Description;
	public PlaylistVisibility Visibility;
	public List<string> VideoIds;
	public string Author;
	public DateTimeOffset LastUpdated;

	public static string GenerateId()
	{
		Random rng = new();
		string playlistId = "LT-PL";
		while (playlistId.Length < 24)
			playlistId += ID_ALPHABET[rng.Next(0, ID_ALPHABET.Length)];
		return playlistId;
	}

	public VideoPlaylistInfo? GetVideoPlaylistInfo(string detailsId, DatabaseUser author, List<DatabaseVideo> videos,
		LocalizationManager localization)
	{
		Playlist pl = new()
		{
			PlaylistId = Id,
			Title = Name,
			TotalVideos = VideoIds.Count,
			CurrentIndex = VideoIds.IndexOf(detailsId),
			LocalCurrentIndex = VideoIds.IndexOf(detailsId),
			LongBylineText = new Text
			{
				Runs =
				{
					new Text.Types.Run
					{
						NavigationEndpoint = new Endpoint
						{
							BrowseEndpoint = new BrowseEndpoint
							{
								BrowseId = author.LTChannelId,
								CanonicalBaseUrl = $"/@LT_{author.UserId}"
							}
						},
						Text = author.UserId
					}
				}
			},
			IsCourse = false,
			IsInfinite = false
		};
		int i = 0;
		// todo: add null checks for uncached videos
		pl.Contents.AddRange(videos.Select(x =>
		{
			if (x is null)
			{
				return new RendererWrapper
				{
					PlaylistPanelVideoRenderer = new PlaylistPanelVideoRenderer
					{
						VideoId = "",
						Title = new Text
						{
							SimpleText = localization.GetRawString("playlist.video.uncached")
						},
						Thumbnail = new Thumbnails
						{
							Thumbnails_ = { new Thumbnail
								{
									Url = "https://i.ytimg.com/vi/___________/hqdefault.jpg",
									Width = 120,
									Height = 90
								}
							}
						},
						LengthText = new Text
						{
							SimpleText = "00:00"
						},
						IndexText = new Text
						{
							SimpleText = (++i).ToString()
						}
					}
				};
			}
			return new RendererWrapper
			{
				PlaylistPanelVideoRenderer = new PlaylistPanelVideoRenderer
				{
					VideoId = x.Id,
					Title = new Text
					{
						SimpleText = x.Title
					},
					Thumbnail = new Thumbnails
					{
						Thumbnails_ = { x.Thumbnails }
					},
					ShortBylineText = new Text
					{
						Runs =
						{
							new Text.Types.Run
							{
								NavigationEndpoint = new Endpoint
								{
									BrowseEndpoint = new BrowseEndpoint
									{
										BrowseId = x.Channel.Id,
										CanonicalBaseUrl = $"/channel/{x.Channel.Id}"
									}
								},
								Text = x.Channel.Name
							}
						}
					},
					LengthText = new Text
					{
						SimpleText = x.Duration
					},
					IndexText = new Text
					{
						SimpleText = (++i).ToString()
					}
				}
			};
		}));
		return new VideoPlaylistInfo(pl, "en");
	}

	public PlaylistHeaderRenderer GetHeaderRenderer(DatabaseUser author, LocalizationManager localization) =>
		new()
		{
			Title = new Text
			{
				SimpleText = Name
			},
			NumVideosText = new Text
			{
				SimpleText = string.Format(localization.GetRawString("videos.count"), VideoIds.Count)
			},
			ViewCountText = new Text
			{
				SimpleText = localization.GetRawString("lighttube.views")
			},
			Byline = new RendererWrapper
			{
				PlaylistBylineRenderer = new PlaylistBylineRenderer
				{
					Text =
					{
						new Text
						{
							SimpleText = string.Format(localization.GetRawString("lastupdated"),
								LastUpdated.ToString("MMMM dd, yyyy"))
						}
					}
				}
			},
			DescriptionText = new Text
			{
				SimpleText = Description
			},
			OwnerText = new Text
			{
				Runs =
				{
					new Text.Types.Run
					{
						NavigationEndpoint = new Endpoint
						{
							BrowseEndpoint = new BrowseEndpoint
							{
								BrowseId = author.LTChannelId,
								CanonicalBaseUrl = $"/@LT_{author.UserId}"
							}
						},
						Text = author.UserId
					}
				}
			},
			CinematicContainer = new RendererWrapper
			{
				CinematicContainerRenderer = new CinematicContainerRenderer
				{
					BackgroundImageConfig = new CinematicContainerRenderer.Types.BackgroundConfig
					{
						Thumbnails = new Thumbnails
						{
							Thumbnails_ =
							{
								new Thumbnail
								{
									Url = $"https://i.ytimg.com/vi/{VideoIds.FirstOrDefault()}/hqdefault.jpg",
									Width = 480,
									Height = 360
								}
							}
						}
					}
				}
			}
		};
}

public enum PlaylistVisibility
{
	Private,
	Unlisted,
	Visible
}