using QuizMaster.Web.Helpers;
using System.ComponentModel.DataAnnotations;

namespace QuizMaster.Web.Pages.Lecturer
{
	/// <summary>
	/// The quiz form of CreateQuiz and EditQuiz. These checks run on the server and match the console's input
	/// limits; the domain checks the rest when the service is called.
	/// </summary>
	public class QuizInput
	{
		[Required(ErrorMessage = "Title can't be empty.")]
		[StringLength(InputLimits.TitleMax, ErrorMessage = "Title can be at most {1} characters long.")]
		public string Title { get; set; } = string.Empty;

		[Required(ErrorMessage = "Topic can't be empty.")]
		[StringLength(InputLimits.TopicMax, ErrorMessage = "Topic can be at most {1} characters long.")]
		public string Topic { get; set; } = string.Empty;

		[Required(ErrorMessage = "Enter the time limit in minutes.")]
		[Range(InputLimits.TimeLimitMin, InputLimits.TimeLimitMax, ErrorMessage = "Time limit must be between {1} and {2} minutes.")]
		public int? TimeLimitMinutes { get; set; }

		[Required(ErrorMessage = "Enter the pass percent.")]
		[Range(InputLimits.PassPercentMin, InputLimits.PassPercentMax, ErrorMessage = "Pass percent must be between {1} and {2}.")]
		public int? PassPercent { get; set; }
	}
}
