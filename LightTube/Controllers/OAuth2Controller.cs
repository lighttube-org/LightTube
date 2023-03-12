using System.Web;
using LightTube.Contexts;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace LightTube.Controllers;

[Route("/oauth2")]
public class OAuth2Controller : Controller
{
	private HttpClient _client = new();

	[Route("authorize")]
	public async Task<IActionResult> Authorize(
		[FromQuery(Name = "response_type")] string responseType,
		[FromQuery(Name = "client_id")] string clientId,
		[FromQuery(Name = "redirect_uri")] string redirectUri,
		[FromQuery(Name = "scope")] string scope,
		[FromQuery(Name = "state")] string? state = null)
	{
		bool valid = true;
		List<string> statuses = new()
		{
			$"Response Type: {responseType}",
			$"Client ID: {clientId}",
			$"Redirect URI: {redirectUri}",
			$"Scopes: {scope}",
			$"State: {state}",
			"========================"
		};

		if (string.IsNullOrEmpty(responseType))
		{
			valid = false;
			return View(new OAuthContext("Response type invalid!"));
		}

		if (responseType != "code")
		{
			valid = false;
			return View(new OAuthContext("response_type must be `code`!"));
		}

		// ...client ids
		// yeah idk how to do that
		// so theyre just the name of the app ig?
		// i dont know how to handle this on something
		// thats not centralized

		if (string.IsNullOrEmpty(clientId))
		{
			valid = false;
			return View(new OAuthContext("client_id cannot be empty"));
		}

		Uri? redirectUrl = null;
		if (string.IsNullOrEmpty(redirectUri))
		{
			valid = false;
			return View(new OAuthContext("redirect_uri is not valid"));
		}
		else
		{
			redirectUrl = new(redirectUri);
		}

		if (scope == null)
		{
			valid = false;
			return View(new OAuthContext($"scope cannot be empty"));
		}
		else
		{
			string[] invalidScopes = Utils.FindInvalidScopes(scope.Split(" "));
			if (invalidScopes.Length > 0)
			{
				valid = false;
				return View(new OAuthContext($"Invalid scope(s): {string.Join(", ", invalidScopes)}"));
			}
		}
		

		statuses.Add(valid ? "Request valid" : "INVALID REQUEST");
		if (valid && redirectUrl != null)
			statuses.Add($"Return URL: {redirectUrl}{(redirectUrl.Query.Length > 0 ? "&" : "?")}code=CODE{(state is not null ? $"&state={state}" : "")}");
		//return Ok(string.Join("\n", statuses));

		OAuthContext ctx = new(HttpContext, clientId, scope.Split(" "));
		if (ctx.User is null) return Redirect("/account/login?redirectUrl=" + HttpUtility.UrlEncode(Request.GetEncodedPathAndQuery()));
		return View(ctx);
	}
}