// ReSharper disable MustUseReturnValue
// TODO: Future me, PLEASE find a way to do these that does NOT include nothing but string interpolation
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using InnerTube.Models;
using Microsoft.AspNetCore.Html;

namespace LightTube
{
	public static class DynamicItemExtensions
	{
		private static Dictionary<Type, MethodInfo> _renderers = new();

		public static void RegisterRenderers()
		{
			foreach (MethodInfo methodInfo in typeof(DynamicItemExtensions).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
				.Where(x => x.Name == "RenderDynamicItem"))
				_renderers.Add(methodInfo.GetParameters()[0].ParameterType, methodInfo);
		}
		
		public static IHtmlContent GetHtml(this DynamicItem item)
		{
			HtmlContentBuilder b = new();
			if (_renderers.ContainsKey(item.GetType()))
				_renderers[item.GetType()].Invoke(null, new object[]
				{
					item,
					b
				});
			else
				_renderers[typeof(DynamicItem)].Invoke(null, new object[]
				{
					item,
					b
				});
			return b;
		}

		private static void RenderDynamicItem(DynamicItem item, IHtmlContentBuilder b)
		{
			#if DEBUG
			b.AppendHtml($"<h1>{item.GetType().Name} {item.Title}</h1>");
			#endif
		}

		private static void RenderDynamicItem(VideoItem video, IHtmlContentBuilder b)
		{
			b.AppendHtml($"<div class=\"video\"><a href=\"/watch?v={video.Id}\" class=\"thumbnail\"style=\"background-image: url('{video.Thumbnails.LastOrDefault()?.Url}')\"><span class=\"video-length\">{video.Duration}</span></a><div class=\"info\"><a href=\"/watch?v={video.Id}\" class=\"title max-lines-2\">{video.Title}</a><div style=\"display: flex; flex-direction: column; row-gap: 8px\"><a href=\"/watch?v={video.Id}\"><span>{video.Views} views</span> <span>•</span> <span>{video.UploadedAt}</span></a><a href=\"/channel/{video.Channel.Id}\"><img alt=\"Channel Avatar\" src=\"{video.Channel.Avatars.LastOrDefault()?.Url}\"> {video.Channel.Name}</a></div></div></div>");
		}

		private static void RenderDynamicItem(ChannelItem channel, IHtmlContentBuilder b)
		{
			b.AppendHtml($"<div class=\"channel\"><a href=\"/channel/{channel.Id}\" class=\"avatar\"><img src=\"{channel.Thumbnails.LastOrDefault()?.Url}\" alt=\"Channel Avatar\"></a><a href=\"/channel/{channel.Id}\" class=\"info\"><span class=\"name max-lines-2\">{channel.Title}</span><div><div><span>{channel.Subscribers}</span><span>•</span><span>{channel.VideoCount} videos</span></div><p>{channel.Description}</p></div></a><button class=\"subscribe-button\" data-cid=\"{channel.Id}\">Subscribe</button></div>");
		}

		private static void RenderDynamicItem(PlaylistItem playlist, IHtmlContentBuilder b)
		{
			b.AppendHtml($"<div class=\"playlist\"><a href=\"/watch?v={playlist.FirstVideoId}&amp;list={playlist.Id}\" class=\"thumbnail\" style=\"background-image: url('{playlist.Thumbnails.LastOrDefault()?.Url}')\"><div><span>{playlist.VideoCount}</span><span>VIDEOS</span></div></a><div class=\"info\"><a href=\"/watch?v={playlist.Id}\" class=\"title max-lines-2\">{playlist.Title}</a><div><a href=\"/channel/{playlist.Channel.Id}\">{playlist.Channel.Name}</a><ul><!--li><a href=\"#\">Video #1 • 4:20</a></li--><li><a href=\"/playlist?list={playlist.Id}\">View Full Playlist</a></li></ul></div></div></div>");
		}

		private static void RenderDynamicItem(RadioItem mix, IHtmlContentBuilder b)
		{
			b.AppendHtml(
				$"<div class=\"playlist\"><a href=\"/watch?v={mix.FirstVideoId}&amp;list={mix.Id}\" class=\"thumbnail\" style=\"background-image: url('{mix.Thumbnails.LastOrDefault()?.Url}')\"><div><span>MIX</span></div></a><div class=\"info\"><a href=\"/watch?v={mix.Id}\" class=\"title max-lines-2\">{mix.Title}</a><div><span>{mix.Channel.Name}</span></div></div></div>");
		}

		private static void RenderDynamicItem(ShelfItem shelf, IHtmlContentBuilder b)
		{
			b.AppendHtml($"<div class=\"shelf\"><hr><h2>{shelf.Title}</h2><div class=\"video-list\">");
			foreach (DynamicItem item in shelf.Items)
			{
				if (_renderers.ContainsKey(item.GetType()))
					_renderers[item.GetType()].Invoke(null, new object[]
					{
						item,
						b
					});
			}
			b.AppendHtml("</div><hr></div>");
		}

		private static void RenderDynamicItem(HorizontalCardListItem cardList, IHtmlContentBuilder b)
		{
			b.AppendHtml($"<div class=\"shelf\"><hr><h2>{cardList.Title}</h2><div class=\"card-list\">");
			foreach (DynamicItem item in cardList.Items)
			{
				if (_renderers.ContainsKey(item.GetType()))
					_renderers[item.GetType()].Invoke(null, new object[]
					{
						item,
						b
					});
			}
			b.AppendHtml("</div><hr></div>");
		}

		private static void RenderDynamicItem(CardItem card, IHtmlContentBuilder b)
		{
			b.AppendHtml(
				$"<a class=\"card\" href=\"/results?search_query={HttpUtility.UrlEncode(card.Title)}\"><img src=\"{card.Thumbnails.LastOrDefault()?.Url}\"><span>{card.Title}</span></a>");
		}
	}
}