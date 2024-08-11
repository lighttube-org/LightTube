using System.Net;
using LightTube.Database;
using LightTube.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LightTube.Attributes;

public class ApiDisableableAttribute(params string[] scopes) : Attribute, IActionFilter
{
    private string[] _scopes = scopes;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (Configuration.ApiEnabled) return;
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Result = new ContentResult();
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}