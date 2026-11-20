using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.DTOs.QuestionDTOs
{
	public class CreateQuestionDTO
	{
		public int QuizId { get; set; }
		public QuestionType Type { get; set; }
		public string Text { get; set; } = string.Empty;
		public int Points { get; set; }

		// Ignored for True/False questions: they always get the options True and False.
		public List<string> Options { get; set; } = new();

		// 0-based option indexes. True/False: 0 = True, 1 = False.
		public List<int> CorrectOptionIndexes { get; set; } = new();
	}
}
