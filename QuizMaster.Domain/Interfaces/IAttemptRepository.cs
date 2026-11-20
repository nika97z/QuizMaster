using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Domain.Interfaces
{
	public interface IAttemptRepository
	{
		void AddAttempt(Attempt attempt);
		void UpdateAttempt(Attempt attempt);
		Attempt? GetAttemptById(int id);
		Attempt? GetActiveAttempt(int studentId, int quizId);
		List<Attempt> GetAttemptsByStudent(int studentId);
		List<Attempt> GetFinishedAttempts();
	}
}
