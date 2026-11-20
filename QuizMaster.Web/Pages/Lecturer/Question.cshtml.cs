using Microsoft.AspNetCore.Mvc;
using QuizMaster.Application.Exceptions;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuestionDTOs;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Web.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace QuizMaster.Web.Pages.Lecturer
{
	/// <summary>
	/// Adds a question (?quizId=1&amp;type=Single) or edits one (?quizId=1&amp;questionId=5). The type is chosen when
	/// adding and fixed when editing. The correct answer is never prefilled: it's private and isn't in any DTO, so the
	/// lecturer enters it again. The services and the domain decide what's valid and report every problem.
	/// </summary>
	public class QuestionModel : QuizMasterPageModel
	{
		private readonly IQuizService _quizService;
		private readonly IQuestionService _questionService;

		public QuestionModel(IQuizService quizService, IQuestionService questionService, IAppLogger logger) : base(logger)
		{
			_quizService = quizService;
			_questionService = questionService;
		}

		[BindProperty(SupportsGet = true)]
		public int QuizId { get; set; }

		[BindProperty(SupportsGet = true)]
		public int? QuestionId { get; set; }

		/// <summary>
		/// The type chosen for a new question. Ignored when editing.
		/// </summary>
		[BindProperty(SupportsGet = true)]
		public QuestionType? Type { get; set; }

		[BindProperty]
		public QuestionInput Input { get; set; } = new();

		public QuizDetailsDTO? Quiz { get; private set; }

		public bool IsEdit => QuestionId.HasValue;

		/// <summary>
		/// The type of the question on the form. Null on the add page until a type is chosen.
		/// </summary>
		public QuestionType? FormType { get; private set; }

		/// <summary>
		/// The query string the form posts back to: the quiz, plus the question (edit) or the type (add).
		/// </summary>
		public Dictionary<string, string> FormRouteValues
		{
			get
			{
				var values = new Dictionary<string, string> { ["quizId"] = QuizId.ToString(CultureInfo.InvariantCulture) };
				if (QuestionId.HasValue)
				{
					values["questionId"] = QuestionId.Value.ToString(CultureInfo.InvariantCulture);
				}
				else if (FormType.HasValue)
				{
					values["type"] = FormType.Value.ToString();
				}
				return values;
			}
		}

		public void OnGet()
		{
			if (!Load(out var existing))
			{
				return;
			}
			if (existing != null)
			{
				// Prefill the text, points and options (in position order), but never the correct answer.
				Input.Text = existing.Text;
				Input.Points = existing.Points;
				Input.Options = existing.Options.OrderBy(o => o.Position).Select(o => (string?)o.Text).ToList();
			}
			PadOptionRows();
		}

		public IActionResult OnPost()
		{
			if (!Load(out var existing))
			{
				return Page();
			}
			if (FormType == null)
			{
				ModelState.AddModelError(string.Empty, "Choose the question type first.");
				return Page();
			}

			var type = FormType.Value;
			var (options, correct) = ReadAnswer(type);
			if (!ModelState.IsValid)
			{
				PadOptionRows();
				return Page();
			}

			var saved = existing == null
				? Execute(() => _questionService.AddQuestion(CurrentUserId, new CreateQuestionDTO
				{
					QuizId = QuizId,
					Type = type,
					Text = Input.Text,
					Points = Input.Points ?? 0,
					Options = options,
					CorrectOptionIndexes = correct
				}))
				: Execute(() => _questionService.UpdateQuestion(CurrentUserId, new UpdateQuestionDTO
				{
					Id = existing.Id,
					Type = type,
					Text = Input.Text,
					Points = Input.Points ?? 0,
					Options = options,
					CorrectOptionIndexes = correct
				}));
			if (!saved)
			{
				PadOptionRows();
				return Page();
			}

			TempData.AddSuccess(existing == null ? "The question was added." : "The question was updated.");
			return RedirectToPage("/Lecturer/Manage", new { quizId = QuizId });
		}

		/// <summary>
		/// Loads the quiz (it must be the lecturer's own) and, when editing, the question. Sets FormType.
		/// </summary>
		private bool Load(out QuestionDTO? existing)
		{
			QuizDetailsDTO? quiz = null;
			QuestionDTO? question = null;
			var loaded = Execute(() =>
			{
				quiz = _quizService.GetQuizDetails(CurrentUserId, QuizId);
				if (QuestionId.HasValue)
				{
					question = quiz.Questions.FirstOrDefault(q => q.Id == QuestionId.Value)
						?? throw new NotFoundException($"Question {QuestionId.Value} is not part of this quiz.");
				}
			});

			existing = question;
			if (!loaded || quiz == null)
			{
				return false;
			}

			Quiz = quiz;
			if (question != null)
			{
				// An existing question keeps its type (the console keeps it too).
				FormType = question.Type;
			}
			else if (Type.HasValue && Enum.IsDefined(Type.Value))
			{
				FormType = Type.Value;
			}
			return true;
		}

		/// <summary>
		/// Turns the form into the options and correct answer the service expects. Blank option rows are dropped, so
		/// CorrectOptionIndexes are indexes into the list without them, not the row numbers on the screen.
		/// </summary>
		private (List<string> Options, List<int> Correct) ReadAnswer(QuestionType type)
		{
			if (type == QuestionType.TrueFalse)
			{
				// True/False has fixed options, so only the answer is sent.
				var answer = Input.TrueFalseAnswer switch
				{
					true => new List<int> { TrueFalseQuestion.TrueIndex },
					false => new List<int> { TrueFalseQuestion.FalseIndex },
					null => new List<int>()
				};
				return (new List<string>(), answer);
			}

			var options = new List<string>();
			var indexOfRow = new Dictionary<int, int>();
			for (var row = 0; row < Input.Options.Count; row++)
			{
				var text = Input.Options[row]?.Trim();
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				if (text.Length > InputLimits.OptionTextMax)
				{
					ModelState.AddModelError(string.Empty, $"Option {row + 1} can be at most {InputLimits.OptionTextMax} characters long.");
				}
				indexOfRow[row] = options.Count;
				options.Add(text);
			}

			var markedRows = type == QuestionType.Single
				? (Input.CorrectRow.HasValue ? new List<int> { Input.CorrectRow.Value } : new List<int>())
				: Input.CorrectRows.Distinct().OrderBy(row => row).ToList();
			var correct = new List<int>();
			foreach (var row in markedRows)
			{
				if (indexOfRow.TryGetValue(row, out var index))
				{
					correct.Add(index);
				}
				else
				{
					// Dropping the mark silently would change the answer the lecturer meant.
					ModelState.AddModelError(string.Empty, $"Option {row + 1} is marked as correct, but it's empty.");
				}
			}
			return (options, correct);
		}

		/// <summary>
		/// The form always shows Question.MaxOptions option rows. Blank rows are ignored when saving.
		/// </summary>
		private void PadOptionRows()
		{
			while (Input.Options.Count < InputLimits.MaxOptions)
			{
				Input.Options.Add(null);
			}
		}

		public class QuestionInput
		{
			[Required(ErrorMessage = "Question text can't be empty.")]
			[StringLength(InputLimits.QuestionTextMax, ErrorMessage = "Question text can be at most {1} characters long.")]
			public string Text { get; set; } = string.Empty;

			[Required(ErrorMessage = "Enter the points.")]
			[Range(InputLimits.PointsMin, InputLimits.PointsMax, ErrorMessage = "Points must be between {1} and {2}.")]
			public int? Points { get; set; }

			/// <summary>
			/// Single and Multi: the option texts, one per row. Blank rows are ignored.
			/// </summary>
			public List<string?> Options { get; set; } = new();

			/// <summary>
			/// Single: the row marked as correct.
			/// </summary>
			public int? CorrectRow { get; set; }

			/// <summary>
			/// Multi: the rows marked as correct.
			/// </summary>
			public List<int> CorrectRows { get; set; } = new();

			/// <summary>
			/// True/False: the correct answer.
			/// </summary>
			public bool? TrueFalseAnswer { get; set; }
		}
	}
}
