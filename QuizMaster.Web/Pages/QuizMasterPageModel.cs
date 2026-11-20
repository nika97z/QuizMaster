using Microsoft.AspNetCore.Mvc.RazorPages;
using QuizMaster.Application.Exceptions;
using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Web.Helpers;

namespace QuizMaster.Web.Pages
{
	/// <summary>
	/// Base class of the pages that call services. Execute() and HandleError() are the web version of the console's
	/// SafeExecutor: known problems become messages in the validation summary (the layout shows it), and errors are
	/// written to the log. Pages only collect input, call a service and show the result.
	/// </summary>
	public abstract class QuizMasterPageModel : PageModel
	{
		public const string GeneralErrorMessage = "Something went wrong, and the error was logged.";

		private readonly IAppLogger _logger;

		protected QuizMasterPageModel(IAppLogger logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// The logged-in user's id, always read from the login cookie, never from a form field, query string or route.
		/// </summary>
		protected int CurrentUserId => User.GetUserId() ?? throw new UnauthorizedActionException("Please log in first.");

		/// <summary>
		/// Runs one service call. Returns false when it failed; the problem is then in ModelState.
		/// </summary>
		protected bool Execute(Action action)
		{
			try
			{
				action();
				return true;
			}
			catch (Exception ex)
			{
				HandleError(ex);
				return false;
			}
		}

		/// <summary>
		/// Turns an exception into messages for the page and logs it, like SafeExecutor does in the console.
		/// </summary>
		protected void HandleError(Exception ex)
		{
			switch (ex)
			{
				case DomainValidationException validation:
					foreach (var error in validation.Errors)
					{
						ModelState.AddModelError(string.Empty, error);
					}
					Log(ex);
					break;

				case InvalidCredentialsException:
					// Already logged by the service as LoginFailed.
					ModelState.AddModelError(string.Empty, ex.Message);
					break;

				case NotFoundException or UnauthorizedActionException or DuplicateUserException
					or InvalidAttemptStateException or ArgumentException or QuizGenerationException:
					ModelState.AddModelError(string.Empty, ex.Message);
					Log(ex);
					break;

				default:
					// In Development the message helps when debugging. A stack trace never appears on a page.
					var showDetails = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
					ModelState.AddModelError(string.Empty, showDetails
						? $"{GeneralErrorMessage} Details: {ex.GetBaseException().Message}"
						: GeneralErrorMessage);
					HttpContext.RequestServices.GetRequiredService<ILogger<QuizMasterPageModel>>()
						.LogError(ex, "Unexpected error on {Path}", Request.Path);
					Log(ex);
					break;
			}
		}

		/// <summary>
		/// Before a redirect: moves the errors in ModelState to TempData, so the next page still shows them.
		/// </summary>
		protected void KeepErrorsForRedirect()
		{
			TempData.AddErrors(ModelState.Values
				.SelectMany(entry => entry.Errors)
				.Select(error => error.ErrorMessage)
				.Where(message => !string.IsNullOrWhiteSpace(message)));
		}

		private void Log(Exception ex)
		{
			_logger.Log(LogEventType.Error, $"{ex.GetType().Name}: {ex.GetBaseException().Message}", User.GetUserId());
		}
	}
}
