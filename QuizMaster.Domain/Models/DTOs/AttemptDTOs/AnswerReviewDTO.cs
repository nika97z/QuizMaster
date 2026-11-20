namespace QuizMaster.Domain.Models.DTOs.AttemptDTOs
{
	/// <summary>
	/// Shown only after an attempt is finished: what the student chose and whether it was right.
	/// It never contains the correct answer itself.
	/// </summary>
	public class AnswerReviewDTO
	{
		public int Number { get; set; }
		public string QuestionText { get; set; } = string.Empty;
		public string YourAnswer { get; set; } = string.Empty;
		public bool IsCorrect { get; set; }
		public int PointsAwarded { get; set; }
		public int Points { get; set; }
	}
}
