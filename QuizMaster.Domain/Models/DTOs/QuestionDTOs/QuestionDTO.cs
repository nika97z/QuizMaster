using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.DTOs.QuestionDTOs
{
	/// <summary>
	/// What the UI may see of a question. There is deliberately no correct-answer field.
	/// </summary>
	public class QuestionDTO
	{
		public int Id { get; set; }
		public int QuizId { get; set; }
		public QuestionType Type { get; set; }
		public string Text { get; set; } = string.Empty;
		public int Points { get; set; }
		public int OrderIndex { get; set; }

		// In display order. Answers are sent back as each option's Position.
		public List<QuestionOptionDTO> Options { get; set; } = new();

		// Text produced by the question's own DisplayQuestion(), with options in the same order as Options.
		public string DisplayText { get; set; } = string.Empty;
	}
}
