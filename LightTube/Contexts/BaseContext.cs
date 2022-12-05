using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LightTube.Contexts;

public class BaseContext
{
	public bool IsMobile;
	public List<IHtmlContent> HeadTags = new();
	public List<IHtmlContent> EndTags = new();
	public bool GuideHidden = false;
	public HttpContext Context;

	public BaseContext(HttpContext context)
	{
		Context = context;
		AddMeta("og:site_name", "lighttube");
		AddMeta("og:type", "website");
		AddMeta("theme-color", "#AA0000");
	}

	public void AddScript(string src)
	{
		TagBuilder script = new("script");
		script.Attributes.Add("src", src);
		EndTags.Add(script);
	}

	public void AddStylesheet(string href)
	{
		TagBuilder stylesheet = new("link");
		stylesheet.Attributes.Add("rel", "stylesheet");
		stylesheet.Attributes.Add("href", href);
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
}