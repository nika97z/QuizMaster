using Microsoft.AspNetCore.Mvc;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.AttemptDTOs;
using QuizMaster.Domain.Models.DTOs.QuestionDTOs;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Web.Helpers;

namespace QuizMaster.Web.Pages.Student
{
	/// <summary>
	/// Takes a quiz one question at a time. The console keeps the session in memory; here every request reads the
	/// attempt from the database (GetAttemptSession): its question order, the answers so far and the deadline.
	/// GET only reads. Answer, Finish and Quit are POSTs followed by a redirect, so Refresh and Back never repeat them.
	/// </summary>
	public class TakeModel : QuizMasterPageModel
	{
		private readonly IAttemptService _attemptService;

		public TakeModel(IAttemptService attemptService, IAppLogger logger) : base(logger)
		{
			_attemptService = attemptService;
		}

		[BindProperty(SupportsGet = true)]
		public int AttemptId { get; set; }

		public AttemptSessionDTO? Session { get; private set; }

		/// <summary>
		/// The question to show now: the first one not answered yet, in the attempt's stored order.
		/// </summary>
		public QuestionDTO? CurrentQuestion => Session?.Questions.FirstOrDefault();

		public bool TimeIsUp { get; private set; }

		/// <summary>
		/// Worked out on the server, because the services compare times with the server's clock. The browser only
		/// counts down from this number; it never parses the deadline (its clock or time zone may differ).
		/// </summary>
		public int SecondsLeft { get; private set; }

		/// <summary>
		/// The options chosen in a rejected submit, so they stay selected when the question is shown again.
		/// </summary>
		public List<int> SelectedPositions { get; private set; } = new();

		public IActionResult OnGet()
		{
			return LoadSession() ?? Page();
		}

		public IActionResult OnPostAnswer(int questionId, List<int>? selected)
		{
			var ended = LoadSession();
			if (ended != null)
			{
				return ended;
			}
			if (Session == null)
			{
				return Page();
			}

			// Accept only an answer to the question this page should show now. The domain replaces an existing answer,
			// so a stale page (for example one reached with Back) would otherwise overwrite an earlier answer.
			if (CurrentQuestion == null || CurrentQuestion.Id != questionId)
			{
				TempData.AddWarning("That answer wasn't saved because the page was out of date. This is the current question.");
				return RedirectToPage(new { attemptId = AttemptId });
			}

			SelectedPositions = selected ?? new List<int>();
			if (!ModelState.IsValid)
			{
				return Page();
			}

			// The values are the options' original positions, so the shuffled display order never affects grading.
			var answer = new SubmitAnswerDTO
			{
				AttemptId = AttemptId,
				QuestionId = questionId,
				SelectedOptionPositions = SelectedPositions
			};
			try
			{
				if (!TryCall(() => _attemptService.SubmitAnswer(CurrentUserId, answer), out ended))
				{
					// For example nothing selected, or two choices on a Single question: nothing was saved,
					// and the same question is shown again with the errors.
					return ended ?? Page();
				}
			}
			catch (TimeLimitExceededException)
			{
				// The answer came after the deadline (the service logged it). Finish with the answers given so far.
				return FinishAndShowResult();
			}

			if (Session.Questions.Count == 1)
			{
				// That was the last unanswered question.
				return FinishAndShowResult();
			}
			return RedirectToPage(new { attemptId = AttemptId });
		}

		public IActionResult OnPostFinish()
		{
			return FinishAndShowResult();
		}

		public IActionResult OnPostQuit()
		{
			if (!TryCall(() => _attemptService.CancelAttempt(CurrentUserId, AttemptId), out var ended))
			{
				return ended ?? RedirectBackWithErrors();
			}
			TempData.AddWarning("The attempt was cancelled.");
			return RedirectToPage("/Student/Quizzes");
		}

		/// <summary>
		/// Reads the attempt. Returns a redirect when the attempt has already ended; otherwise null, and either
		/// Session is set or ModelState says why it couldn't be read.
		/// </summary>
		private IActionResult? LoadSession()
		{
			AttemptSessionDTO? session = null;
			if (!TryCall(() => session = _attemptService.GetAttemptSession(CurrentUserId, AttemptId), out var ended) || session == null)
			{
				return ended;
			}

			Session = session;
			var timeLeft = session.Deadline - DateTime.Now;
			TimeIsUp = timeLeft <= TimeSpan.Zero;
			SecondsLeft = TimeIsUp ? 0 : (int)Math.Ceiling(timeLeft.TotalSeconds);
			return null;
		}

		private IActionResult FinishAndShowResult()
		{
			AttemptResultDTO? result = null;
			if (!TryCall(() => result = _attemptService.FinishAttempt(CurrentUserId, AttemptId), out var ended) || result == null)
			{
				return ended ?? RedirectBackWithErrors();
			}

			if (result.TimedOut)
			{
				TempData.AddWarning("Time is up! The quiz is finished with the answers you gave.");
			}
			ResultModel.RememberEmailOutcome(TempData, result.AttemptId, result.EmailSent);
			return RedirectToPage("/Student/Result", new { attemptId = result.AttemptId });
		}

		/// <summary>
		/// Runs one attempt call. When the attempt is no longer in progress (Back after finishing, a second Finish,
		/// another tab), 'ended' is the redirect to show instead. Every other problem goes to HandleError, the web
		/// version of SafeExecutor. TimeLimitExceededException is left to the caller.
		/// </summary>
		private bool TryCall(Action call, out IActionResult? ended)
		{
			ended = null;
			try
			{
				call();
				return true;
			}
			catch (TimeLimitExceededException)
			{
				throw;
			}
			catch (InvalidAttemptStateException ex)
			{
				ended = RedirectToEndedAttempt(ex.Message);
				return false;
			}
			catch (Exception ex)
			{
				HandleError(ex);
				return false;
			}
		}

		/// <summary>
		/// A finished attempt goes to its result. Otherwise (cancelled) the student goes to My attempts with the reason.
		/// </summary>
		private IActionResult RedirectToEndedAttempt(string reason)
		{
			AttemptDTO? attempt = null;
			Execute(() => attempt = _attemptService.GetStudentAttempts(CurrentUserId).FirstOrDefault(a => a.Id == AttemptId));
			if (attempt?.Status == AttemptStatus.Finished)
			{
				return RedirectToPage("/Student/Result", new { attemptId = AttemptId });
			}

			TempData.AddWarning(reason);
			KeepErrorsForRedirect();
			return RedirectToPage("/Student/Attempts");
		}

		private IActionResult RedirectBackWithErrors()
		{
			KeepErrorsForRedirect();
			return RedirectToPage(new { attemptId = AttemptId });
		}
	}
}
