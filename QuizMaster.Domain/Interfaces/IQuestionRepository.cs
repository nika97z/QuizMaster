using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Domain.Interfaces
{
	public interface IQuestionRepository
	{
		void AddQuestion(Question question);
		void UpdateQuestion(Question question);
		void DeleteQuestion(Question question);
		Question? GetQuestionById(int id);
		bool HasAnswers(int questionId);
	}
}
