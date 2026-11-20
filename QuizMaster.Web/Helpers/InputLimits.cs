using QuizMaster.Application.Services.Implementations;
using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Web.Helpers
{
	/// <summary>
	/// Form limits, the same as the console's input checks and the database column sizes. The domain doesn't check
	/// text lengths or the points maximum (the console's input prompts do), so the pages check these on the server
	/// before calling a service. Everything else is decided by the services and the domain.
	/// </summary>
	public static class InputLimits
	{
		public const int UsernameMin = UserService.MinUsernameLength;
		public const int UsernameMax = UserService.MaxUsernameLength;
		public const int EmailMax = 256;
		public const int PasswordMin = UserService.MinPasswordLength;

		public const int TitleMax = Quiz.MaxTitleLength;
		public const int TopicMax = Quiz.MaxTopicLength;
		public const int TimeLimitMin = Quiz.MinTimeLimitMinutes;
		public const int TimeLimitMax = Quiz.MaxTimeLimitMinutes;
		public const int PassPercentMin = Quiz.MinPassPercent;
		public const int PassPercentMax = Quiz.MaxPassPercent;

		public const int QuestionTextMax = Question.MaxTextLength;
		public const int OptionTextMax = QuestionOption.MaxTextLength;
		public const int PointsMin = 1;
		public const int PointsMax = 100;
		public const int MaxOptions = Question.MaxOptions;

		public const int AiQuestionsMin = QuizGenerationService.MinQuestions;
		public const int AiQuestionsMax = QuizGenerationService.MaxQuestions;
		public const int AiSubjectMax = QuizGenerationService.MaxSubjectLength;
	}
}
