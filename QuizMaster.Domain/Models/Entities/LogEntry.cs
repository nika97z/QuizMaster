using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.Entities
{
	/// <summary>
	/// One line of the activity log. The ids are plain columns (no foreign keys), so log rows outlive deleted data.
	/// </summary>
	public class LogEntry
	{
		public int Id { get; set; }
		public DateTime Timestamp { get; set; } = DateTime.Now;
		public LogEventType EventType { get; set; }
		public string Message { get; set; } = string.Empty;
		public int? UserId { get; set; }
		public int? QuizId { get; set; }
		public int? AttemptId { get; set; }
	}
}
