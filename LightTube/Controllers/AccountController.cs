using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Web;
using LightTube.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InnerTube;

namespace LightTube.Controllers
{
	public class AccountController : Controller
	{
		private readonly Youtube _youtube;

		public AccountController(Youtube youtube)
		{
			_youtube = youtube;
		}

		[Route("/Account")]
		public IActionResult Account()
		{
			return Redirect(HttpContext.TryGetUser(out LTUser _) ? "/" : "/Account/Login");
		}

		[HttpGet]
		public IActionResult Login(string err = null)
		{
			if (HttpContext.TryGetUser(out LTUser _))
				return Redirect("/");

			return View(new MessageContext
			{
				Message = err,
				MobileLayout = Utils.IsClientMobile(Request)
			});
		}

		[HttpPost]
		public async Task<IActionResult> Login(string email, string password)
		{
			if (HttpContext.TryGetUser(out LTUser _))
				return Redirect("/");

			try
			{
				LTLogin login = await DatabaseManager.CreateToken(email, password, Request.Headers["user-agent"]);
				Response.Cookies.Append("token", login.Token, new CookieOptions
				{
					Expires = DateTimeOffset.MaxValue
				});

				return Redirect("/");
			}
			catch (KeyNotFoundException e)
			{
				return Redirect("/Account/Login?err=" + HttpUtility.UrlEncode(e.Message));
			}
			catch (UnauthorizedAccessException e)
			{
				return Redirect("/Account/Login?err=" + HttpUtility.UrlEncode(e.Message));
			}
		}
		
		public async Task<IActionResult> Logout()
		{
			if (HttpContext.Request.Cookies.TryGetValue("token", out string token))
			{
				await DatabaseManager.RemoveToken(token);
			}

			HttpContext.Response.Cookies.Delete("token");
			HttpContext.Response.Cookies.Delete("account_data");
			return Redirect("/");
		}

		[HttpGet]
		public IActionResult Register(string err = null)
		{
			if (HttpContext.TryGetUser(out LTUser _))
				return Redirect("/");

			return View(new MessageContext
			{
				Message = err,
				MobileLayout = Utils.IsClientMobile(Request)
			});
		}

		[HttpPost]
		public async Task<IActionResult> Register(string email, string password)
		{
			if (HttpContext.TryGetUser(out LTUser _))
				return Redirect("/");

			try
			{
				await DatabaseManager.CreateUser(email, password);
				LTLogin login = await DatabaseManager.CreateToken(email, password, Request.Headers["user-agent"]);
				Response.Cookies.Append("token", login.Token, new CookieOptions
				{
					Expires = DateTimeOffset.MaxValue
				});

				return Redirect("/");
			}
			catch (DuplicateNameException e)
			{
				return Redirect("/Account/Register?err=" + HttpUtility.UrlEncode(e.Message));
			}
		}

		public IActionResult RegisterLocal()
		{
			if (!HttpContext.TryGetUser(out LTUser _))
				HttpContext.CreateLocalAccount();
			
			return Redirect("/");
		}

		[HttpGet]
		public IActionResult Delete(string err = null)
		{
			if (!HttpContext.TryGetUser(out LTUser _))
				return Redirect("/");

			return View(new MessageContext
			{
				Message = err,
				MobileLayout = Utils.IsClientMobile(Request)
			});
		}

		[HttpPost]
		public async Task<IActionResult> Delete(string email, string password)
		{
			try
			{
				if (email == "Local Account" && password == "local_account")
					Response.Cookies.Delete("account_data");
				else
					await DatabaseManager.DeleteUser(email, password);
				return Redirect("/Account/Register?err=Account+deleted");
			}
			catch (KeyNotFoundException e)
			{
				return Redirect("/Account/Delete?err=" + HttpUtility.UrlEncode(e.Message));
			}
			catch (UnauthorizedAccessException e)
			{
				return Redirect("/Account/Delete?err=" + HttpUtility.UrlEncode(e.Message));
			}
		}

		public async Task<IActionResult> Logins()
		{
			if (!HttpContext.Request.Cookies.TryGetValue("token", out string token))
				return Redirect("/Account/Login");

			return View(new LoginsContext
			{
				Logins = await DatabaseManager.GetAllUserTokens(token),
				MobileLayout = Utils.IsClientMobile(Request)
			});
		}

		public async Task<IActionResult> Subscribe(string channel)
		{
			if (!HttpContext.Request.Cookies.TryGetValue("token", out string token))
				return Unauthorized();

			try
			{
				(LTChannel channel, bool subscribed) result =
					await DatabaseManager.SubscribeToChannel(token, await _youtube.GetChannelAsync(channel));
				return Ok(result.subscribed ? "true" : "false");
			}
			catch
			{
				return Unauthorized();
			}
		}

		public async Task<IActionResult> SubscriptionsJson()
		{
			if (!HttpContext.Request.Cookies.TryGetValue("token", out string token))
				return Json(Array.Empty<string>());
			try
			{
				LTUser user = await DatabaseManager.GetUserFromToken(token);
				return Json(user.SubscribedChannels);
			}
			catch
			{
				return Json(Array.Empty<string>());
			}
		}
	}
}