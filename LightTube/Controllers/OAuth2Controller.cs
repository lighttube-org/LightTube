using System.Web;
using LightTube.ApiModels;
using LightTube.Attributes;
using LightTube.Contexts;
using LightTube.Database;
using LightTube.Database.Models;
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
			return View(new OAuthContext("This instance does not allow OAuth2"));
		if (string.IsNullOrEmpty(responseType))
			return View(new OAuthContext("response_type cannot be empty"));

		if (responseType != "code")
			return View(new OAuthContext("response_type must be `code`"));

		// ...client ids
		// yeah idk how to do that
		// so theyre just the name of the app ig?
		// i dont know how to handle this on something
		// thats not centralized

		if (string.IsNullOrEmpty(clientId))
			return View(new OAuthContext("client_id cannot be empty"));

		Uri? redirectUrl = null;
		if (string.IsNullOrEmpty(redirectUri))
			return View(new OAuthContext("redirect_uri is not valid"));
		redirectUrl = new(redirectUri);

		if (scope == null)
			return View(new OAuthContext("scope cannot be empty"));

		string[] invalidScopes = Utils.FindInvalidScopes(scope.Split(" "));
		if (invalidScopes.Length > 0)
			return View(new OAuthContext($"Invalid scope(s): {string.Join(", ", invalidScopes)}"));


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
		if (!Configuration.OauthEnabled)
			throw new Exception("Instance doesn't allow OAuth");

		if (string.IsNullOrEmpty(responseType))
			throw new Exception("Response type invalid!");

		if (responseType != "code")
			throw new Exception("response_type must be `code`!");

		// ...client ids
		// yeah idk how to do that
		// so theyre just the name of the app ig?
		// i dont know how to handle this on something
		// thats not centralized

		if (string.IsNullOrEmpty(clientId))
			throw new Exception("client_id cannot be empty");

		Uri? redirectUrl = null;
		if (string.IsNullOrEmpty(redirectUri))
			throw new Exception("redirect_uri is not valid");

		redirectUrl = new(redirectUri);

		if (scope == null)
			throw new Exception("scope cannot be empty");

		string[] invalidScopes = Utils.FindInvalidScopes(scope.Split(" "));
		if (invalidScopes.Length > 0)
			throw new Exception($"Invalid scope(s): {string.Join(", ", invalidScopes)}");

		BaseContext ctx = new(HttpContext);
		if (ctx.User is null)
			throw new Exception("User not logged in");

		if (redirectUrl == null)
			throw new Exception("redirect_uri is not valid");
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