using QuizMaster.Domain.Models.DTOs.AttemptDTOs;

namespace QuizMaster.Application.Services.Interfaces
{
	public interface IAttemptService
	{
		AttemptSessionDTO StartAttempt(int studentId, int quizId);
		AttemptSessionDTO GetAttemptSession(int studentId, int attemptId);
		void SubmitAnswer(int studentId, SubmitAnswerDTO answer);
		AttemptResultDTO FinishAttempt(int studentId, int attemptId);
		void CancelAttempt(int studentId, int attemptId);
		List<AttemptDTO> GetStudentAttempts(int studentId);
		AttemptResultDTO GetAttemptResult(int studentId, int attemptId);
	}
}
