using InnerTube.Models;
using InnerTube.Protobuf;
using InnerTube.Renderers;
using LightTube.Database.Models;

namespace LightTube.ApiModels;

public class ApiChannel
{
	public ChannelHeader? Header { get; }
	public ChannelTab[] Tabs { get; }
	public ChannelMetadataRenderer? Metadata { get; }
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

	public ApiChannel(DatabaseUser channel)
	{
		throw new Exception("empty ctors not implemented yet");
		/*
		Id = channel.LTChannelID;
		Title = channel.UserID;
		Avatars = [];
		Banner = [];
		Badges = [];
		PrimaryLinks = [];
		SecondaryLinks = [];
		SubscriberCountText = "LightTube account";
		EnabledTabs =
		[
			ChannelTabs.Playlists.ToString()
		];
		Contents = [channel.PlaylistRenderers()];
		Continuation = null;
		*/
	}
}