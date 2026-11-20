namespace QuizMaster.Domain.Models.Entities
{
	/// <summary>
	/// One answer option. Position is the option's original 0-based index; answers always refer to positions,
	/// so shuffling the display order never changes grading.
	/// </summary>
	public class QuestionOption
	{
		// Column size in the database (see QuestionConfiguration).
		public const int MaxTextLength = 300;

		public int Id { get; set; }
		public int QuestionId { get; set; }
		public int Position { get; set; }
		public string Text { get; set; } = string.Empty;
	}
}
