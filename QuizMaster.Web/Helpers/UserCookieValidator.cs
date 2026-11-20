using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using QuizMaster.Application.Exceptions;
using QuizMaster.Application.Services.Interfaces;
using System.Security.Claims;

namespace QuizMaster.Web.Helpers
{
	/// <summary>
	/// Checks the login cookie against the Users table on every request. A cookie stays valid when the app is pointed
	/// at another database (a test database, or a recreated QuizMasterDb), and by then its user id may belong to
	/// someone else. If the user doesn't exist, or the username or role differ, the cookie is rejected and the browser
	/// is logged out.
	/// </summary>
	public class UserCookieValidator : CookieAuthenticationEvents
	{
		private readonly IUserService _userService;

		public UserCookieValidator(IUserService userService)
		{
			_userService = userService;
		}

		public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
		{
			if (context.Principal == null || !MatchesStoredUser(context.Principal))
			{
				context.RejectPrincipal();
				await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			}
		}

		private bool MatchesStoredUser(ClaimsPrincipal principal)
		{
			var userId = principal.GetUserId();
			if (userId == null)
			{
				return false;
			}

			try
			{
				var user = _userService.GetUserById(userId.Value);
				return user.Username == principal.Identity?.Name && user.Role.ToString() == principal.GetRole();
			}
			catch (Exception ex) when (ex is NotFoundException or ArgumentException)
			{
				return false;
			}
			catch (Exception)
			{
				// The database can't be reached. Keep the user logged in: the page then reports the problem
				// through its normal error handling, and the services still check every call.
				return true;
			}
		}
	}
}
