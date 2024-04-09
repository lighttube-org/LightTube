using LightTube.Database;
using LightTube.Database.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LightTube.Contexts;

public class BaseContext
{
	public string Title;
	public bool IsMobile;
	public List<IHtmlContent> HeadTags = new();
	public List<IHtmlContent> rssElement = new();
	public List<IHtmlContent> EndTags = new();
	public bool GuideHidden = false;
	public HttpContext Context;
	public DatabaseUser? User;

	public BaseContext(HttpContext context)
	{
		Context = context;
		User = DatabaseManager.Users.GetUserFromToken(context.Request.Cookies["token"] ?? "").Result;
		AddMeta("og:site_name", "lighttube");
		AddMeta("og:type", "website");
		AddMeta("theme-color", "#AA0000");
	}

	public void AddScript(string src)
	{
		TagBuilder script = new("script");
		script.Attributes.Add("src", src + "?v=" + Utils.GetVersion());
		EndTags.Add(script);
	}

	public void AddStylesheet(string href)
	{
		TagBuilder stylesheet = new("link");
		stylesheet.Attributes.Add("rel", "stylesheet");
		stylesheet.Attributes.Add("href", href + "?v=" + Utils.GetVersion());
		HeadTags.Add(stylesheet);
	}
	public void AddRSSUrl(string href)
	{
		TagBuilder rss = new("link");
		rss.Attributes.Add("rel", "alternate");
		rss.Attributes.Add("type", "application/rss+xml");
		rss.Attributes.Add("title", "RSS");
		rss.Attributes.Add("href", href);
		rssElement.Add(rss);
	}
	public void AddMeta(string property, string content)
	{
		TagBuilder stylesheet = new("meta");
		stylesheet.Attributes.Add("property", property);
		stylesheet.Attributes.Add("content", content);
		HeadTags.Add(stylesheet);
	}

	public string? GetSearchBoxInput()
	{
		if (this is SearchContext s) return s.Query;
		return Context.Request.Cookies.TryGetValue("lastSearch", out string? q) ? q : null;
	}

	public string GetThemeClass() =>
		Context.Request.Cookies.TryGetValue("theme", out string? theme)
			? $"theme-{theme}"
			: $"theme-{Configuration.DefaultTheme}";
}