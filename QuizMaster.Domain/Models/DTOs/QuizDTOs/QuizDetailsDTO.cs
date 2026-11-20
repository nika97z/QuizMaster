using QuizMaster.Domain.Models.DTOs.QuestionDTOs;

namespace QuizMaster.Domain.Models.DTOs.QuizDTOs
{
	public class QuizDetailsDTO : QuizDTO
	{
		public List<QuestionDTO> Questions { get; set; } = new();
	}
}
