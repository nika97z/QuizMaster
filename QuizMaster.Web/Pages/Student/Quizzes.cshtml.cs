using Microsoft.AspNetCore.Mvc;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.AttemptDTOs;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Web.Helpers;

namespace QuizMaster.Web.Pages.Student
{
	/// <summary>
	/// The quiz catalog. Starting a quiz is a POST from the Start button only: StartAttempt can cancel an expired
	/// attempt and start a new one, so a GET (a reload, a link) must never run it.
	/// </summary>
	public class QuizzesModel : QuizMasterPageModel
	{
		private readonly IQuizService _quizService;
		private readonly IAttemptService _attemptService;

		public QuizzesModel(IQuizService quizService, IAttemptService attemptService, IAppLogger logger) : base(logger)
		{
			_quizService = quizService;
			_attemptService = attemptService;
		}

		public List<QuizDTO> Quizzes { get; private set; } = new();

		public void OnGet()
		{
			Execute(() => Quizzes = _quizService.GetPublishedQuizzes());
		}

		public IActionResult OnPostStart(int quizId)
		{
			AttemptSessionDTO? session = null;
			if (!Execute(() => session = _attemptService.StartAttempt(CurrentUserId, quizId)) || session == null)
			{
				KeepErrorsForRedirect();
				return RedirectToPage();
			}

			if (session.Resumed)
			{
				TempData.AddWarning($"You already had an unfinished attempt, so it continues ({session.AnsweredCount} of {session.TotalQuestions} answered).");
			}
			return RedirectToPage("/Student/Take", new { attemptId = session.AttemptId });
		}
	}
}
