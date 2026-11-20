using Microsoft.AspNetCore.Mvc;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.UserDTOs;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Web.Helpers;
using System.ComponentModel.DataAnnotations;

namespace QuizMaster.Web.Pages.Account
{
	/// <summary>
	/// Anyone can register as a student or a lecturer. Email format checking is turned off for now (see CLAUDE.md),
	/// so the email is a plain text field, and the password is asked once.
	/// </summary>
	public class RegisterModel : QuizMasterPageModel
	{
		private readonly IUserService _userService;

		public RegisterModel(IUserService userService, IAppLogger logger) : base(logger)
		{
			_userService = userService;
		}

		[BindProperty]
		public RegisterInput Input { get; set; } = new();

		public void OnGet()
		{
		}

		public IActionResult OnPost()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			var registration = new RegisterUserDTO
			{
				Username = Input.Username,
				Email = Input.Email,
				Password = Input.Password,
				Role = Input.Role
			};
			UserDTO? user = null;
			if (!Execute(() => user = _userService.Register(registration)) || user == null)
			{
				return Page();
			}

			TempData.AddSuccess($"Account created for {user.Username} ({user.Role}). You can log in now.");
			return RedirectToPage("/Account/Login");
		}

		public class RegisterInput
		{
			[Required(ErrorMessage = "Username can't be empty.")]
			[StringLength(InputLimits.UsernameMax, MinimumLength = InputLimits.UsernameMin,
				ErrorMessage = "Username must be {2}-{1} characters long.")]
			public string Username { get; set; } = string.Empty;

			// A plain text field on purpose: email format checking is off for now (no [EmailAddress]).
			[Required(ErrorMessage = "Email can't be empty.")]
			[StringLength(InputLimits.EmailMax, ErrorMessage = "Email can be at most {1} characters long.")]
			public string Email { get; set; } = string.Empty;

			[Required(ErrorMessage = "Password must be at least 6 characters long.")]
			[MinLength(InputLimits.PasswordMin, ErrorMessage = "Password must be at least {1} characters long.")]
			public string Password { get; set; } = string.Empty;

			[EnumDataType(typeof(UserRole), ErrorMessage = "Choose Student or Lecturer.")]
			public UserRole Role { get; set; } = UserRole.Student;
		}
	}
}
