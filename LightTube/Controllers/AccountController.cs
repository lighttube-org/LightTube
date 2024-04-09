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
        View(new AccountContext
        {
            Redirect = redirectUrl
        });

    private static readonly string[] scopes = ["web"];

    [Route("register")]
    [HttpPost]
    public async Task<IActionResult> Register(string? redirectUrl, string userId, string password, string passwordCheck,
        string? remember)
    {
        AccountContext ac = new()
        {
            Redirect = redirectUrl,
            UserID = userId
        };

        if (!Configuration.RegistrationEnabled)
            return View(ac);

        if (userId is null || password is null || passwordCheck is null)
            ac.Error = "Invalid request";
        else
        {
            if (userId.Except(Utils.UserIdAlphabet).Any())
                ac.Error = "User ID contains invalid character(s)";

            if (password != passwordCheck)
                ac.Error = "Passwords do not match";

            if (password.Length < 8)
                ac.Error = "Password must be longer than 8 characters";

            if (password.Contains(':') || password.Contains(' '))
                ac.Error = "Invalid password";
        }

        if (ac.Error == null)
            try
            {
                await DatabaseManager.Users.CreateUser(userId, password);
                DatabaseLogin login = await DatabaseManager.Users.CreateToken(userId, password, Request.Headers.UserAgent.First(),
                    scopes);

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
        View(new AccountContext
        {
            Redirect = redirectUrl
        });

    [Route("login")]
    [HttpPost]
    public async Task<IActionResult> Login(string? redirectUrl, string userId, string password, string? remember)
    {
        AccountContext ac = new()
        {
            Redirect = redirectUrl,
            UserID = userId
        };

        if (userId is null || password is null)
            ac.Error = "Invalid request";

        if (ac.Error == null)
            try
            {
                DatabaseLogin login =
                    await DatabaseManager.Users.CreateToken(userId, password, Request.Headers.UserAgent,
                        scopes);

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

    [Route("delete")]
    [HttpGet]
    public IActionResult Delete(string? redirectUrl) =>
        View(new AccountContext
        {
            Redirect = redirectUrl
        });

    [Route("delete")]
    [HttpPost]
    public async Task<IActionResult> Delete(string? redirectUrl, string userId, string password, string passwordCheck, string? consent)
    {
        AccountContext ac = new()
        {
            Redirect = redirectUrl,
            UserID = userId
        };

        if (userId is null || password is null || passwordCheck is null)
            ac.Error = "Invalid request";
        else
        {
            if (password != passwordCheck)
                ac.Error = "Passwords don't match";

            if (consent is null)
                ac.Error = "Please check the checkbox to delete your account";
        }

        if (ac.Error == null)
            try
            {
                await DatabaseManager.Users.DeleteUser(userId, password);

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
        await DatabaseManager.Users.RemoveToken(Request.Headers["token"]);
        Response.Cookies.Delete("token");
        return Redirect(redirectUrl ?? "/");
    }
}