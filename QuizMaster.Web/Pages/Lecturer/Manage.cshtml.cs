using Microsoft.AspNetCore.Mvc;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Web.Helpers;

namespace QuizMaster.Web.Pages.Lecturer
{
	/// <summary>
	/// One quiz: its details and questions, publish/close, remove a question, delete the quiz. Each change is a POST
	/// followed by a redirect back here, with the outcome in TempData, so Refresh never repeats it.
	/// </summary>
	public class ManageModel : QuizMasterPageModel
	{
		private readonly IQuizService _quizService;
		private readonly IQuestionService _questionService;

		public ManageModel(IQuizService quizService, IQuestionService questionService, IAppLogger logger) : base(logger)
		{
			_quizService = quizService;
			_questionService = questionService;
		}

		[BindProperty(SupportsGet = true)]
		public int QuizId { get; set; }

		public QuizDetailsDTO? Quiz { get; private set; }

		public void OnGet()
		{
			Execute(() => Quiz = _quizService.GetQuizDetails(CurrentUserId, QuizId));
		}

		public IActionResult OnPostPublish()
		{
			// If the quiz isn't valid yet, every problem comes back in DomainValidationException.Errors and is listed.
			return Change(() => _quizService.PublishQuiz(CurrentUserId, QuizId), "The quiz is published. Students can take it now.");
		}

		public IActionResult OnPostClose()
		{
			return Change(() => _quizService.CloseQuiz(CurrentUserId, QuizId), "The quiz is closed. Students can't start it, and you can edit it again.");
		}

		public IActionResult OnPostRemoveQuestion(int questionId)
		{
			return Change(() => _questionService.RemoveQuestion(CurrentUserId, questionId), "The question was removed.");
		}

		public IActionResult OnPostDelete()
		{
			if (!Execute(() => _quizService.DeleteQuiz(CurrentUserId, QuizId)))
			{
				KeepErrorsForRedirect();
				return RedirectToPage(new { quizId = QuizId });
			}
			TempData.AddSuccess("The quiz was deleted.");
			return RedirectToPage("/Lecturer/Quizzes");
		}

		private IActionResult Change(Action change, string successMessage)
		{
			if (Execute(change))
			{
				TempData.AddSuccess(successMessage);
			}
			else
			{
				KeepErrorsForRedirect();
			}
			return RedirectToPage(new { quizId = QuizId });
		}
	}
}
