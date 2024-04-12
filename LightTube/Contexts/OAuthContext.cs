using LightTube.Database;
using LightTube.Database.Models;

namespace LightTube.Contexts;

public class OAuthContext : AccountContext
{
	public DatabaseUser? User;
	public HttpContext Context;
	public string Name;
	public string[] Scopes;

	public OAuthContext(HttpContext context, string error, params object[] format) : base(context)
	{
		HtmlTitle = Localization.GetRawString("oauth2.title");
		Error = string.Format(Localization.GetRawString(error), format);
	}

	public OAuthContext(HttpContext context, string name, string[] scopes) : base(context)
	{
		Context = context;
		User = DatabaseManager.Users.GetUserFromToken(context.Request.Cookies["token"] ?? "").Result;
		HtmlTitle = Localization.GetRawString("oauth2.title");
		Name = name;
		Scopes = scopes;
	}
}