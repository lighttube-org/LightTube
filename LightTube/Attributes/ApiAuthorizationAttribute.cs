using System.Net;
using LightTube.ApiModels;
using LightTube.Database;
using LightTube.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LightTube.Attributes;

public class ApiAuthorizationAttribute(params string[] scopes) : Attribute, IActionFilter
{
    private string[] _scopes = scopes;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        DatabaseOauthToken? login = DatabaseManager.Oauth2.GetLoginFromHttpContext(context.HttpContext).Result;
        if (login != null && _scopes.All(scope => login.Scopes.Contains(scope))) return;

        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Result = new JsonResult(new ApiResponse<object>("UNAUTHORIZED", "Unauthorized", 401));
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}