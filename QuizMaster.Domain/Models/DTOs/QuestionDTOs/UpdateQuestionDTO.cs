using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.DTOs.QuestionDTOs
{
	public class UpdateQuestionDTO
	{
		public int Id { get; set; }
		public QuestionType Type { get; set; }
		public string Text { get; set; } = string.Empty;
		public int Points { get; set; }
		public List<string> Options { get; set; } = new();
		public List<int> CorrectOptionIndexes { get; set; } = new();
	}
}
