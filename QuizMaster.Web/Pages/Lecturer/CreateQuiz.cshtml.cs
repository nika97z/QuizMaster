using Microsoft.AspNetCore.Mvc;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Web.Helpers;

namespace QuizMaster.Web.Pages.Lecturer
{
	/// <summary>
	/// Two ways to create a quiz: fill in the form yourself, or let the AI write it (on a subject you choose, or a
	/// random one). Each form posts to its own handler and binds only its own fields, so the other form's checks
	/// don't run.
	/// </summary>
	public class CreateQuizModel : QuizMasterPageModel
	{
		private readonly IQuizService _quizService;
		private readonly IQuizGenerationService _generationService;

		public CreateQuizModel(IQuizService quizService, IQuizGenerationService generationService, IAppLogger logger) : base(logger)
		{
			_quizService = quizService;
			_generationService = generationService;
		}

		/// <summary>
		/// The "Create it yourself" form.
		/// </summary>
		public QuizInput Input { get; set; } = new();

		/// <summary>
		/// The "Let AI create it" form.
		/// </summary>
		public AiQuizInput Ai { get; set; } = new();

		public bool AiAvailable => _generationService.IsAvailable;

		public void OnGet()
		{
		}

		public IActionResult OnPost(QuizInput input)
		{
			Input = input;
			if (!ModelState.IsValid)
			{
				return Page();
			}

			var newQuiz = new CreateQuizDTO
			{
				Title = input.Title,
				Topic = input.Topic,
				TimeLimitMinutes = input.TimeLimitMinutes ?? 0,
				PassPercent = input.PassPercent ?? 0
			};
			QuizDTO? quiz = null;
			if (!Execute(() => quiz = _quizService.CreateQuiz(CurrentUserId, newQuiz)) || quiz == null)
			{
				return Page();
			}

			TempData.AddSuccess($"\"{quiz.Title}\" was created as a draft. Add questions, then publish it.");
			return RedirectToPage("/Lecturer/Manage", new { quizId = quiz.Id });
		}

		public IActionResult OnPostGenerate(AiQuizInput ai)
		{
			Ai = ai;
			if (!ModelState.IsValid)
			{
				return Page();
			}

			var request = new GenerateQuizDTO
			{
				Subject = ai.Subject ?? string.Empty,
				RandomSubject = ai.RandomSubject,
				QuestionCount = ai.QuestionCount ?? 0,
				TimeLimitMinutes = ai.TimeLimitMinutes ?? 0,
				PassPercent = ai.PassPercent ?? 0
			};
			QuizGenerationResultDTO? result = null;
			if (!Execute(() => result = _generationService.GenerateQuiz(CurrentUserId, request)) || result == null)
			{
				return Page();
			}

			TempData.AddSuccess($"The AI wrote \"{result.Title}\" about {result.Subject} with {result.QuestionCount} questions. "
				+ "It's a draft: check the questions and answers, then publish it.");
			if (result.SkippedQuestions > 0)
			{
				TempData.AddWarning($"{result.SkippedQuestions} of the AI's questions didn't pass the checks, so they were left out.");
			}
			return RedirectToPage("/Lecturer/Manage", new { quizId = result.QuizId });
		}
	}
}
