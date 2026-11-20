using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuizMaster.Web.Helpers;

namespace QuizMaster.Web.Pages
{
	public class IndexModel : PageModel
	{
		/// <summary>
		/// Logged-in users go straight to their role's start page.
		/// </summary>
		public IActionResult OnGet()
		{
			if (User.Identity?.IsAuthenticated == true)
			{
				return RedirectToPage(User.StartPage());
			}
			return Page();
		}
	}
}
