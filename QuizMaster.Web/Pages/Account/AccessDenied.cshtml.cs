using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuizMaster.Web.Pages.Account
{
	/// <summary>
	/// Where a logged-in user lands after opening a page of the other role.
	/// </summary>
	public class AccessDeniedModel : PageModel
	{
		public void OnGet()
		{
			Response.StatusCode = StatusCodes.Status403Forbidden;
		}
	}
}
