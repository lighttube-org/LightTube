using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using LightTube.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using InnerTube;
using InnerTube.Models;

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
			return View(new BaseContext
			{
				MobileLayout = Utils.IsClientMobile(Request)
			});
		}

		[HttpGet]
		public IActionResult Login(string err = null)
		{
			if (HttpContext.TryGetUser(out LTUser _, "web"))
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
			if (HttpContext.TryGetUser(out LTUser _, "web"))
				return Redirect("/");

			try
			{
				LTLogin login = await DatabaseManager.CreateToken(email, password, Request.Headers["user-agent"], new []{"web"});
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
			if (HttpContext.TryGetUser(out LTUser _, "web"))
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
			if (HttpContext.TryGetUser(out LTUser _, "web"))
				return Redirect("/");

			try
			{
				await DatabaseManager.CreateUser(email, password);
				LTLogin login = await DatabaseManager.CreateToken(email, password, Request.Headers["user-agent"], new []{"web"});
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
			if (!HttpContext.TryGetUser(out LTUser _, "web"))
				HttpContext.CreateLocalAccount();
			
			return Redirect("/");
		}

		[HttpGet]
		public IActionResult Delete(string err = null)
		{
			if (!HttpContext.TryGetUser(out LTUser _, "web"))
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
			if (!HttpContext.TryGetUser(out LTUser user, "web"))
				return Unauthorized();

			try
			{
				YoutubeChannel youtubeChannel = await _youtube.GetChannelAsync(channel, ChannelTabs.About);
				
				(LTChannel channel, bool subscribed) result;
				result.channel = await DatabaseManager.UpdateChannel(youtubeChannel.Id, youtubeChannel.Name, youtubeChannel.Subscribers,
					youtubeChannel.Avatars.First().Url.ToString());
				
				if (user.PasswordHash == "local_account")
				{
					LTChannel ltChannel = await DatabaseManager.UpdateChannel(youtubeChannel.Id, youtubeChannel.Name, youtubeChannel.Subscribers,
						youtubeChannel.Avatars.First().Url.ToString());
					if (user.SubscribedChannels.Contains(ltChannel.ChannelId))
						user.SubscribedChannels.Remove(ltChannel.ChannelId);
					else
						user.SubscribedChannels.Add(ltChannel.ChannelId);

					HttpContext.Response.Cookies.Append("account_data", JsonConvert.SerializeObject(user),
						new CookieOptions
						{
							Expires = DateTimeOffset.MaxValue
						});

					result.subscribed = user.SubscribedChannels.Contains(ltChannel.ChannelId);
				}
				else
				{
					result =
						await DatabaseManager.SubscribeToChannel(user, youtubeChannel);
				}
				return Ok(result.subscribed ? "true" : "false");
			}
			catch
			{
				return Unauthorized();
			}
		}

		public IActionResult SubscriptionsJson()
		{
			if (!HttpContext.TryGetUser(out LTUser user, "web"))
				return Json(Array.Empty<string>());
			try
			{
				return Json(user.SubscribedChannels);
			}
			catch
			{
				return Json(Array.Empty<string>());
			}
		}

		public async Task<IActionResult> ContentSettings()
		{
			YoutubeLocals locals = await _youtube.GetLocalsAsync();
			return View(new LocalsContext
			{
				Languages = locals.Languages,
				Regions = locals.Regions,
				CurrentLanguage = HttpContext.GetLanguage(),
				CurrentRegion = HttpContext.GetRegion(),
				MobileLayout = Utils.IsClientMobile(Request)
			});
		}

		public IActionResult SetContentSettings(string type, string value)
		{
			switch (type)
			{
				case "language":
					HttpContext.Response.Cookies.Append("hl", value);
					break;
				case "region":
					HttpContext.Response.Cookies.Append("gl", value);
					break;
			}
			return Redirect("/Account/ContentSettings");
		}
	}
}