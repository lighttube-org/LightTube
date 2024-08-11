using LightTube.Contexts;
using LightTube.Database;
using LightTube.Database.Models;
using Microsoft.AspNetCore.Mvc;

namespace LightTube.Controllers;

[Route("/account")]
public class AccountController : Controller
{
    [Route("register")]
    [HttpGet]
    public IActionResult Register(string? redirectUrl) =>
        View(new AccountContext(HttpContext)
        {
            Redirect = redirectUrl
        });

    private static readonly string[] scopes = ["web"];

    [Route("register")]
    [HttpPost]
    public async Task<IActionResult> Register(string? redirectUrl, string? userId, string? password, string? passwordCheck,
        string? remember)
    {
        AccountContext ac = new(HttpContext)
        {
            Redirect = redirectUrl,
            UserID = userId
        };

        if (!Configuration.RegistrationEnabled)
            return View(ac);

        if (userId is null || password is null || passwordCheck is null)
            ac.Error = ac.Localization.GetRawString("error.request.invalid");
        else
        {
            if (userId.Except(Utils.UserIdAlphabet).Any())
                ac.Error = ac.Localization.GetRawString("error.register.useridinvalidchars");

            if (password != passwordCheck)
                ac.Error = ac.Localization.GetRawString("error.register.password.match");

            if (password.Length < 8)
                ac.Error = ac.Localization.GetRawString("error.register.password.length");

            if (password.Contains(':') || password.Contains(' '))
                ac.Error = ac.Localization.GetRawString("error.register.password.invalid");
        }

        if (ac.Error == null)
            try
            {
                await DatabaseManager.Users.CreateUser(userId!, password!);
                DatabaseLogin login = await DatabaseManager.Users.CreateToken(userId!, password!,
                    Request.Headers.UserAgent.First()!, scopes);

                Response.Cookies.Append("token", login.Token, new CookieOptions
                {
                    Expires = remember is null ? null : DateTimeOffset.MaxValue
                });

                return Redirect(redirectUrl ?? "/");
            }
            catch (Exception e)
            {
                ac.Error = e.Message;
            }

        return View(ac);
    }

    [Route("login")]
    [HttpGet]
    public IActionResult Login(string? redirectUrl) =>
        View(new AccountContext(HttpContext)
        {
            Redirect = redirectUrl
        });

    [Route("login")]
    [HttpPost]
    public async Task<IActionResult> Login(string? redirectUrl, string? userId, string? password, string? remember)
    {
        AccountContext ac = new(HttpContext)
        {
            Redirect = redirectUrl,
            UserID = userId
        };

        if (userId is null || password is null)
            ac.Error = ac.Localization.GetRawString("error.request.invalid");

        if (ac.Error == null)
            try
            {
                DatabaseLogin login =
                    await DatabaseManager.Users.CreateToken(userId!, password!, Request.Headers.UserAgent!, scopes);

                Response.Cookies.Append("token", login.Token, new CookieOptions
                {
                    Expires = remember is null ? null : DateTimeOffset.MaxValue
                });

                return Redirect(redirectUrl ?? "/");
            }
            catch (Exception e)
            {
                ac.Error = ac.Localization.GetRawString(e.Message);
            }

        return View(ac);
    }

    [Route("delete")]
    [HttpGet]
    public IActionResult Delete(string? redirectUrl) =>
        View(new AccountContext(HttpContext)
        {
            Redirect = redirectUrl
        });

    [Route("delete")]
    [HttpPost]
    public async Task<IActionResult> Delete(string? redirectUrl, string? userId, string? password, string? passwordCheck, string? consent)
    {
        AccountContext ac = new(HttpContext)
        {
            Redirect = redirectUrl,
            UserID = userId
        };

        if (userId is null || password is null || passwordCheck is null)
            ac.Error = ac.Localization.GetRawString("error.request.invalid");
        else
        {
            if (password != passwordCheck)
                ac.Error = ac.Localization.GetRawString("error.register.password.match");

            if (consent is null)
                ac.Error = ac.Localization.GetRawString("error.delete.consent");
        }

        if (ac.Error == null)
            try
            {
                await DatabaseManager.Users.DeleteUser(userId!, password!);

                Response.Cookies.Delete("token");

                return Redirect(redirectUrl ?? "/");
            }
            catch (Exception e)
            {
                ac.Error = e.Message;
            }

        return View(ac);
    }

    [Route("logout")]
    public async Task<IActionResult> Logout(string? redirectUrl)
    {
        await DatabaseManager.Users.RemoveToken(Request.Cookies["token"]!);
        Response.Cookies.Delete("token");
        return Redirect(redirectUrl ?? "/");
    }
}