using System.Web;
using LightTube.ApiModels;
using LightTube.Attributes;
using LightTube.Contexts;
using LightTube.Database;
using LightTube.Database.Models;
using LightTube.Localization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers;

[Route("/oauth2")]
public class OAuth2Controller : Controller
{
    [Route("authorize")]
    [HttpGet]
    public async Task<IActionResult> Authorize(
        [FromQuery(Name = "response_type")] string responseType,
        [FromQuery(Name = "client_id")] string clientId,
        [FromQuery(Name = "redirect_uri")] string redirectUri,
        [FromQuery(Name = "scope")] string? scope,
        [FromQuery(Name = "state")] string? state = null)
    {
        if (!Configuration.OauthEnabled)
            return View(new OAuthContext(HttpContext, "error.oauth2.disabled"));
        if (string.IsNullOrEmpty(responseType))
            return View(new OAuthContext(HttpContext, "error.oauth2.response_type.empty"));

        if (responseType != "code")
            return View(new OAuthContext(HttpContext, "error.oauth2.response_type.code"));

        // ...client ids
        // yeah idk how to do that
        // so theyre just the name of the app ig?
        // i dont know how to handle this on something
        // thats not centralized

        if (string.IsNullOrEmpty(clientId))
            return View(new OAuthContext(HttpContext, "error.oauth2.client_id.empty"));

        Uri? redirectUrl = null;
        if (string.IsNullOrEmpty(redirectUri))
            return View(new OAuthContext(HttpContext, "error.oauth2.redirect_uri.invalid"));
        redirectUrl = new(redirectUri);

        if (scope == null)
            return View(new OAuthContext(HttpContext, "error.oauth2.scope.empty"));

        string[] invalidScopes = Utils.FindInvalidScopes(scope.Split(" "));
        if (invalidScopes.Length > 0)
            return View(new OAuthContext(HttpContext, error: "error.oauth2.scope.invalid", format: string.Join(", ", invalidScopes)));


        OAuthContext ctx = new(HttpContext, clientId, scope.Split(" "));
        if (ctx.User is null)
            return Redirect("/account/login?redirectUrl=" + HttpUtility.UrlEncode(Request.GetEncodedPathAndQuery()));
        return View(ctx);
    }

    [Route("authorize")]
    [HttpPost]
    public async Task<IActionResult> GetAuthTokenAndRedirect(
        [FromQuery(Name = "response_type")] string responseType,
        [FromQuery(Name = "client_id")] string clientId,
        [FromQuery(Name = "redirect_uri")] string redirectUri,
        [FromQuery(Name = "scope")] string scope,
        [FromQuery(Name = "state")] string? state = null)
    {
        LocalizationManager localization = LocalizationManager.GetFromHttpContext(HttpContext);
        if (!Configuration.OauthEnabled)
            throw new Exception(localization.GetRawString("error.oauth2.disabled"));
        
        if (string.IsNullOrEmpty(responseType))
            throw new Exception(localization.GetRawString("error.oauth2.response_type.empty"));

        if (responseType != "code")
            throw new Exception(localization.GetRawString("error.oauth2.response_type.code"));

        // ...client ids
        // yeah idk how to do that
        // so theyre just the name of the app ig?
        // i dont know how to handle this on something
        // thats not centralized

        if (string.IsNullOrEmpty(clientId))
            throw new Exception(localization.GetRawString("error.oauth2.client_id.empty"));

        Uri? redirectUrl = null;
        if (string.IsNullOrEmpty(redirectUri))
            throw new Exception(localization.GetRawString("error.oauth2.redirect_uri.invalid"));

        redirectUrl = new(redirectUri);

        if (scope == null)
            throw new Exception(localization.GetRawString("error.oauth2.scope.empty"));

        string[] invalidScopes = Utils.FindInvalidScopes(scope.Split(" "));
        if (invalidScopes.Length > 0)
            throw new Exception(localization.GetRawString("error.oauth2.scope.invalid"));

        BaseContext ctx = new(HttpContext);
        if (ctx.User is null)
            throw new Exception(localization.GetRawString("error.oauth2.user.loggedout"));

        if (redirectUrl == null)
            throw new Exception(localization.GetRawString("error.oauth2.redirect_uri.invalid"));
        string returnUrl =
            $"{redirectUrl}{(redirectUrl.Query.Length > 0 ? "&" : "?")}code=%%%CODE%%%{(state is not null ? $"&state={state}" : "")}";
        string code =
            await DatabaseManager.Oauth2.CreateOauthToken(Request.Cookies["token"]!, clientId, scope.Split(" "));


        return Redirect(returnUrl.Replace("%%%CODE%%%", code));
    }

    [Route("token")]
    [HttpPost]
    public async Task<IActionResult> GrantTokenAsync(
        [FromForm(Name = "grant_type")] string grantType,
        [FromForm(Name = "code")] string code,
        [FromForm(Name = "refresh_token")] string refreshToken,
        [FromForm(Name = "redirect_uri")] string redirectUri,
        [FromForm(Name = "client_id")] string clientId,
        [FromForm(Name = "client_secret")] string clientSecret)
    {
        if (!Configuration.OauthEnabled)
            return Unauthorized();
        if (grantType is not ("code" or "authorization_code" or "refresh_token"))
            return Unauthorized();
        DatabaseOauthToken? token = await DatabaseManager.Oauth2.RefreshToken(refreshToken ?? code, clientId);
        if (token is null) return Unauthorized();
        return Json(new Oauth2CodeGrantResponse(token));
    }
}