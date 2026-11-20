namespace QuizMaster.Domain.Models.DTOs.QuizDTOs
{
	/// <summary>
	/// The draft quiz the AI created.
	/// </summary>
	public class QuizGenerationResultDTO
	{
		public int QuizId { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Subject { get; set; } = string.Empty;
		public int QuestionCount { get; set; }

		/// <summary>
		/// Questions the AI wrote that didn't pass the checks, so they were left out.
		/// </summary>
		public int SkippedQuestions { get; set; }
	}
}
