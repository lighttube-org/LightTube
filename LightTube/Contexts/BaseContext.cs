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

	[Obsolete("Use GetThemClass instead")]
	public bool IsDarkMode()
	{
		if (Context.Request.Cookies.TryGetValue("theme", out string? theme)) return theme == "dark";
		return Configuration.GetVariable("LIGHTTUBE_DEFAULT_THEME", "light") == "dark";
	}

	public string GetThemeClass() =>
		Context.Request.Cookies.TryGetValue("theme", out string? theme)
			? $"theme-{theme}"
			: $"theme-{Configuration.GetVariable("LIGHTTUBE_DEFAULT_THEME", "light")}";
}