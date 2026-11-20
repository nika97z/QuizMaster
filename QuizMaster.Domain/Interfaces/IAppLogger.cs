using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Interfaces
{
	/// <summary>
	/// The "Logger" of the assignment. Implementations must never throw.
	/// </summary>
	public interface IAppLogger
	{
		void Log(LogEventType eventType, string message, int? userId = null, int? quizId = null, int? attemptId = null);
		List<LogEntry> GetRecentEntries(int count);
	}
}
