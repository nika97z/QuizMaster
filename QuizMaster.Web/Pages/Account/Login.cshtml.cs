using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.UserDTOs;
using QuizMaster.Web.Helpers;
using System.ComponentModel.DataAnnotations;

namespace QuizMaster.Web.Pages.Account
{
	public class LoginModel : QuizMasterPageModel
	{
		private readonly IUserService _userService;

		public LoginModel(IUserService userService, IAppLogger logger) : base(logger)
		{
			_userService = userService;
		}

		[BindProperty]
		public LoginInput Input { get; set; } = new();

		/// <summary>
		/// The page the user wanted before being sent to the login page.
		/// </summary>
		[BindProperty(SupportsGet = true)]
		public string? ReturnUrl { get; set; }

		public IActionResult OnGet()
		{
			if (User.Identity?.IsAuthenticated == true)
			{
				return RedirectToPage(User.StartPage());
			}
			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			var login = new LoginDTO { Username = Input.Username, Password = Input.Password };
			UserDTO? user = null;
			if (!Execute(() => user = _userService.Login(login)) || user == null)
			{
				return Page();
			}

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, UserClaims.CreatePrincipal(user));
			TempData.AddSuccess($"Welcome, {user.Username}!");

			// Only a local address, so a crafted link can't send the user to another site after logging in.
			if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
			{
				return LocalRedirect(ReturnUrl);
			}
			return RedirectToPage(UserClaims.StartPage(user.Role));
		}

		public class LoginInput
		{
			[Required(ErrorMessage = "Username can't be empty.")]
			[StringLength(InputLimits.UsernameMax, ErrorMessage = "Username can be at most {1} characters long.")]
			public string Username { get; set; } = string.Empty;

			[Required(ErrorMessage = "Password can't be empty.")]
			public string Password { get; set; } = string.Empty;
		}
	}
}
