using Microsoft.AspNetCore.Mvc;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Web.Helpers;

namespace QuizMaster.Web.Pages.Lecturer
{
	public class EditQuizModel : QuizMasterPageModel
	{
		private readonly IQuizService _quizService;

		public EditQuizModel(IQuizService quizService, IAppLogger logger) : base(logger)
		{
			_quizService = quizService;
		}

		[BindProperty(SupportsGet = true)]
		public int QuizId { get; set; }

		[BindProperty]
		public QuizInput Input { get; set; } = new();

		/// <summary>
		/// False when the quiz couldn't be loaded (not found, or another lecturer's quiz): the form isn't shown.
		/// </summary>
		public bool ShowForm { get; private set; }

		public bool IsPublished { get; private set; }

		public void OnGet()
		{
			QuizDetailsDTO? quiz = null;
			if (!Execute(() => quiz = _quizService.GetQuizDetails(CurrentUserId, QuizId)) || quiz == null)
			{
				return;
			}

			Input = new QuizInput
			{
				Title = quiz.Title,
				Topic = quiz.Topic,
				TimeLimitMinutes = quiz.TimeLimitMinutes,
				PassPercent = quiz.PassPercent
			};
			IsPublished = quiz.IsPublished;
			ShowForm = true;
		}

		public IActionResult OnPost()
		{
			// The form was submitted, so show it again (with the user's values) if anything is wrong.
			ShowForm = true;
			if (!ModelState.IsValid)
			{
				return Page();
			}

			var changes = new UpdateQuizDTO
			{
				Id = QuizId,
				Title = Input.Title,
				Topic = Input.Topic,
				TimeLimitMinutes = Input.TimeLimitMinutes ?? 0,
				PassPercent = Input.PassPercent ?? 0
			};
			if (!Execute(() => _quizService.UpdateQuiz(CurrentUserId, changes)))
			{
				return Page();
			}

			TempData.AddSuccess("The quiz was updated.");
			return RedirectToPage("/Lecturer/Manage", new { quizId = QuizId });
		}
	}
}
