using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.DTOs.AttemptDTOs
{
	public class AttemptDTO
	{
		public int Id { get; set; }
		public int QuizId { get; set; }
		public string QuizTitle { get; set; } = string.Empty;
		public DateTime StartedAt { get; set; }
		public DateTime? FinishedAt { get; set; }
		public AttemptStatus Status { get; set; }
		public int Score { get; set; }
		public int MaxScore { get; set; }
		public decimal Percent { get; set; }
		public AttemptResult Result { get; set; }
		public string? CertificateCode { get; set; }
	}
}
