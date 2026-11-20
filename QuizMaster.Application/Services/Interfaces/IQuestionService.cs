using QuizMaster.Domain.Models.DTOs.QuestionDTOs;

namespace QuizMaster.Application.Services.Interfaces
{
	public interface IQuestionService
	{
		QuestionDTO AddQuestion(int lecturerId, CreateQuestionDTO question);
		QuestionDTO UpdateQuestion(int lecturerId, UpdateQuestionDTO question);
		void RemoveQuestion(int lecturerId, int questionId);
	}
}
