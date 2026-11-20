using QuizMaster.Domain.Models.DTOs.QuestionDTOs;

namespace QuizMaster.Domain.Models.DTOs.QuizDTOs
{
	/// <summary>
	/// What the AI wrote. Like a lecturer's CreateQuestionDTO, the questions carry their correct answers: they go
	/// straight into QuestionFactory and are never shown to anyone.
	/// </summary>
	public class GeneratedQuizDTO
	{
		public string Title { get; set; } = string.Empty;
		public string Topic { get; set; } = string.Empty;
		public List<CreateQuestionDTO> Questions { get; set; } = new();
	}
}
