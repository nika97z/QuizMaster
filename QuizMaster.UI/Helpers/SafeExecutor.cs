using QuizMaster.Application.Exceptions;
using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.UI.Helpers
{
	/// <summary>
	/// Wraps every menu action in try/catch. Known problems become friendly messages; every error is written
	/// to the log. A bad input or a lost database connection never crashes the app.
	/// </summary>
	public class SafeExecutor
	{
		private readonly IAppLogger _logger;

		public SafeExecutor(IAppLogger logger)
		{
			_logger = logger;
		}

		public bool Execute(Action action, int? userId = null)
		{
			try
			{
				action();
				return true;
			}
			catch (EndOfInputException)
			{
				throw;
			}
			catch (DomainValidationException ex)
			{
				if (ex.Errors.Count == 1)
				{
					ConsoleHelper.Error(ex.Errors[0]);
				}
				else
				{
					ConsoleHelper.Error("Please fix the following:");
					foreach (var error in ex.Errors)
					{
						ConsoleHelper.Error($"  - {error}");
					}
				}
				Log(ex, userId);
			}
			catch (InvalidCredentialsException ex)
			{
				// Already logged by the service as LoginFailed.
				ConsoleHelper.Error(ex.Message);
			}
			catch (Exception ex) when (ex is NotFoundException or UnauthorizedActionException or DuplicateUserException
				or InvalidAttemptStateException or ArgumentException or QuizGenerationException)
			{
				ConsoleHelper.Error(ex.Message);
				Log(ex, userId);
			}
			catch (Exception ex)
			{
				ConsoleHelper.Error("Something went wrong, and the error was logged.");
				ConsoleHelper.Error($"Details: {ex.GetBaseException().Message}");
				Log(ex, userId);
			}
			return false;
		}

		private void Log(Exception ex, int? userId)
		{
			_logger.Log(LogEventType.Error, $"{ex.GetType().Name}: {ex.GetBaseException().Message}", userId);
		}
	}
}
