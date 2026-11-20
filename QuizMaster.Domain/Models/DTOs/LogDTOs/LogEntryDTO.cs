using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.DTOs.LogDTOs
{
	public class LogEntryDTO
	{
		public int Id { get; set; }
		public DateTime Timestamp { get; set; }
		public LogEventType EventType { get; set; }
		public string Message { get; set; } = string.Empty;
		public int? UserId { get; set; }
		public int? QuizId { get; set; }
		public int? AttemptId { get; set; }
	}
}
