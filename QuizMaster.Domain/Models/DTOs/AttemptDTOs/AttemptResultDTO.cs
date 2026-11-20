using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.DTOs.AttemptDTOs
{
	public class AttemptResultDTO
	{
		public int AttemptId { get; set; }
		public int QuizId { get; set; }
		public string QuizTitle { get; set; } = string.Empty;
		public int Score { get; set; }
		public int MaxScore { get; set; }
		public decimal Percent { get; set; }
		public int PassPercent { get; set; }
		public int CorrectCount { get; set; }
		public int IncorrectCount { get; set; }
		public int TotalQuestions { get; set; }
		public AttemptResult Result { get; set; }
		public AttemptStatus Status { get; set; }
		public string? CertificateCode { get; set; }
		public DateTime StartedAt { get; set; }
		public DateTime? FinishedAt { get; set; }
		public bool TimedOut { get; set; }
		public bool EmailSent { get; set; }
		public List<AnswerReviewDTO> Review { get; set; } = new();
	}
}
