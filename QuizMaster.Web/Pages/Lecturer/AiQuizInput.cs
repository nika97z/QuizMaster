using QuizMaster.Web.Helpers;
using System.ComponentModel.DataAnnotations;

namespace QuizMaster.Web.Pages.Lecturer
{
	/// <summary>
	/// The "Let AI create it" form on CreateQuiz. QuizGenerationService checks everything again, including that a
	/// subject is given unless a random subject was chosen.
	/// </summary>
	public class AiQuizInput
	{
		[StringLength(InputLimits.AiSubjectMax, ErrorMessage = "The subject can be at most {1} characters long.")]
		public string? Subject { get; set; }

		public bool RandomSubject { get; set; }

		[Required(ErrorMessage = "Enter the number of questions.")]
		[Range(InputLimits.AiQuestionsMin, InputLimits.AiQuestionsMax, ErrorMessage = "The number of questions must be between {1} and {2}.")]
		public int? QuestionCount { get; set; } = 5;

		[Required(ErrorMessage = "Enter the time limit in minutes.")]
		[Range(InputLimits.TimeLimitMin, InputLimits.TimeLimitMax, ErrorMessage = "Time limit must be between {1} and {2} minutes.")]
		public int? TimeLimitMinutes { get; set; } = 10;

		[Required(ErrorMessage = "Enter the pass percent.")]
		[Range(InputLimits.PassPercentMin, InputLimits.PassPercentMax, ErrorMessage = "Pass percent must be between {1} and {2}.")]
		public int? PassPercent { get; set; } = 60;
	}
}
