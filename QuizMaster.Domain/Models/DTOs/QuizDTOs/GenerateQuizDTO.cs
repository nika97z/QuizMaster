namespace QuizMaster.Domain.Models.DTOs.QuizDTOs
{
	/// <summary>
	/// A lecturer's request to let the AI write a quiz: the subject (or a random one), how many questions,
	/// and the usual quiz settings.
	/// </summary>
	public class GenerateQuizDTO
	{
		public string Subject { get; set; } = string.Empty;
		public bool RandomSubject { get; set; }
		public int QuestionCount { get; set; }
		public int TimeLimitMinutes { get; set; }
		public int PassPercent { get; set; }
	}
}
