using LightTube.Database;
using LightTube.Database.Models;

namespace LightTube.Contexts;

public class OAuthContext : AccountContext
{
    public DatabaseUser? User;
    public HttpContext Context;
    public string Name;
    public string[] Scopes;

    public OAuthContext(string error)
    {
        HtmlTitle = "Authorize application";
        Error = error;
    }

    public OAuthContext(HttpContext context, string name, string[] scopes)
    {
        Context = context;
        User = DatabaseManager.Users.GetUserFromToken(context.Request.Cookies["token"] ?? "").Result;
        HtmlTitle = "Authorize application";
        Name = name;
        Scopes = scopes;
    }
}