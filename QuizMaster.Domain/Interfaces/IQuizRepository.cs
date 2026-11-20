using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Domain.Interfaces
{
	public interface IQuizRepository
	{
		void AddQuiz(Quiz quiz);
		void UpdateQuiz(Quiz quiz);
		void DeleteQuiz(Quiz quiz);
		Quiz? GetQuizById(int id);
		Quiz? GetQuizWithQuestions(int id);
		List<Quiz> GetPublishedQuizzes();
		List<Quiz> GetQuizzesByLecturer(int lecturerId);
		List<Quiz> GetAllQuizzesWithQuestions();
		bool HasAttempts(int quizId);
	}
}
