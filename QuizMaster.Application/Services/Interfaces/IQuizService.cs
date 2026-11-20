using QuizMaster.Domain.Models.DTOs.QuizDTOs;

namespace QuizMaster.Application.Services.Interfaces
{
	public interface IQuizService
	{
		List<QuizDTO> GetPublishedQuizzes();
		List<QuizDTO> GetLecturerQuizzes(int lecturerId);
		QuizDetailsDTO GetQuizDetails(int lecturerId, int quizId);
		QuizDTO CreateQuiz(int lecturerId, CreateQuizDTO quiz);
		void UpdateQuiz(int lecturerId, UpdateQuizDTO quiz);
		void PublishQuiz(int lecturerId, int quizId);
		void CloseQuiz(int lecturerId, int quizId);
		void DeleteQuiz(int lecturerId, int quizId);
	}
}
