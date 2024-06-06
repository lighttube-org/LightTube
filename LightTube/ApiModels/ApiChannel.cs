using InnerTube.Models;
using InnerTube.Protobuf;
using InnerTube.Renderers;
using LightTube.Database.Models;
using LightTube.Localization;
using Endpoint = InnerTube.Protobuf.Endpoint;

namespace LightTube.ApiModels;

public class ApiChannel
{
	public ChannelHeader? Header { get; }
	public ChannelTab[] Tabs { get; }
	public ChannelMetadata? Metadata { get; }
	public RendererContainer[] Contents { get; }

	public ApiChannel(InnerTubeChannel channel)
	{
		Header = channel.Header;
		Tabs = channel.Tabs.ToArray();
		Metadata = channel.Metadata;
		Contents = channel.Contents;
	}

	public ApiChannel(ContinuationResponse continuation)
	{
		Header = null;
		Tabs = [];
		Metadata = null;
		List<RendererContainer> renderers = new();
		renderers.AddRange(continuation.Results);
		if (continuation.ContinuationToken != null)
			renderers.Add(new RendererContainer
			{
				Type = "continuation",
				OriginalType = "continuationItemRenderer",
				Data = new ContinuationRendererData
				{
					ContinuationToken = continuation.ContinuationToken
				}
			});
		Contents = renderers.ToArray();
	}

	public ApiChannel(DatabaseUser channel, LocalizationManager localization)
	{
		Header = new ChannelHeader(new PageHeaderRenderer
		{
			PageTitle = channel.UserId,
			Content = new RendererWrapper
			{
				PageHeaderViewModel = new PageHeaderViewModel
				{
					Image = new RendererWrapper
					{
						DecoratedAvatarViewModel = new DecoratedAvatarViewModel
						{
							Avatar = new RendererWrapper
							{
								AvatarViewModel = new AvatarViewModel
								{
									Image = new Image()
								}
							}
						},
						ImageBannerViewModel = new ImageBannerViewModel
						{
							Image = new Image()
						}
					},
					Metadata = new RendererWrapper
					{
						ContentMetadataViewModel = new ContentMetadataViewModel
						{
							MetadataRows =
							{
								new ContentMetadataViewModel.Types.MetadataRow
								{
									MetadataParts =
									{
										new ContentMetadataViewModel.Types.MetadataRow.Types.
											AttributedDescriptionWrapper
											{
												Text = new AttributedDescription
												{
													Content = $"@LT_{channel.UserId}"
												}
											}
									}
								},
								new ContentMetadataViewModel.Types.MetadataRow
								{
									MetadataParts =
									{
										new ContentMetadataViewModel.Types.MetadataRow.Types.
											AttributedDescriptionWrapper
											{
												Text = new AttributedDescription
												{
													Content = "LightTube Channel"
												}
											},
										new ContentMetadataViewModel.Types.MetadataRow.Types.
											AttributedDescriptionWrapper
											{
												Text = new AttributedDescription
												{
													Content = ""
												}
											}
									}
								}
							}
						}
					},
					Description = new RendererWrapper
					{
						DescriptionPreviewViewModel = new DescriptionPreviewViewModel
						{
							Content = new AttributedDescription
							{
								Content = ""
							}
						}
					}
				}
			}
		}, channel.LTChannelId, "en");
		Tabs =
		[
			new ChannelTab(new TabRenderer
			{
				Endpoint = new Endpoint
				{
					BrowseEndpoint = new BrowseEndpoint
					{
						Params = "EglwbGF5bGlzdHPyBgQKAkIA"
					}
				},
				Title = "Playlists",
				Selected = true
			})
		];
		Metadata = null;
		Contents = channel.PlaylistRenderers(localization).ToArray();
	}
}