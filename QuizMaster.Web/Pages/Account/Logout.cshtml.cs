using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuizMaster.Web.Helpers;

namespace QuizMaster.Web.Pages.Account
{
	/// <summary>
	/// Logging out is POST only (the navbar's button), so a link or an image can't log anyone out.
	/// </summary>
	public class LogoutModel : PageModel
	{
		public IActionResult OnGet()
		{
			return RedirectToPage("/Index");
		}

		public async Task<IActionResult> OnPostAsync()
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			TempData.AddSuccess("You are logged out.");
			return RedirectToPage("/Index");
		}
	}
}
