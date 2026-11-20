using Microsoft.AspNetCore.Authentication.Cookies;
using QuizMaster.Domain.Models.DTOs.UserDTOs;
using QuizMaster.Domain.Models.Enums;
using System.Globalization;
using System.Security.Claims;

namespace QuizMaster.Web.Helpers
{
	/// <summary>
	/// The claims in the login cookie: user id, username and role. The acting user's id is always read from here,
	/// never from a form field, query string or route.
	/// </summary>
	public static class UserClaims
	{
		public const string LecturerRole = nameof(UserRole.Lecturer);
		public const string StudentRole = nameof(UserRole.Student);
		public const string LecturerPolicy = "LecturerOnly";
		public const string StudentPolicy = "StudentOnly";

		public static ClaimsPrincipal CreatePrincipal(UserDTO user)
		{
			var claims = new List<Claim>
			{
				new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
				new(ClaimTypes.Name, user.Username),
				new(ClaimTypes.Role, user.Role.ToString())
			};
			return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
		}

		public static int? GetUserId(this ClaimsPrincipal user)
		{
			var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;
		}

		public static string? GetRole(this ClaimsPrincipal user)
		{
			return user.FindFirst(ClaimTypes.Role)?.Value;
		}

		public static bool IsLecturer(this ClaimsPrincipal user)
		{
			return user.IsInRole(LecturerRole);
		}

		/// <summary>
		/// The page a role starts on: the quiz catalog for students, the lecturer's own quizzes for lecturers.
		/// </summary>
		public static string StartPage(UserRole role)
		{
			return role == UserRole.Lecturer ? "/Lecturer/Quizzes" : "/Student/Quizzes";
		}

		public static string StartPage(this ClaimsPrincipal user)
		{
			return StartPage(user.IsLecturer() ? UserRole.Lecturer : UserRole.Student);
		}
	}
}
