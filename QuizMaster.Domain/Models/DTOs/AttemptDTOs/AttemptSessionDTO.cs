using QuizMaster.Domain.Models.DTOs.QuestionDTOs;

namespace QuizMaster.Domain.Models.DTOs.AttemptDTOs
{
	/// <summary>
	/// A started (or resumed) attempt: the questions still to answer, already in shuffled order.
	/// </summary>
	public class AttemptSessionDTO
	{
		public int AttemptId { get; set; }
		public int QuizId { get; set; }
		public string QuizTitle { get; set; } = string.Empty;
		public DateTime StartedAt { get; set; }
		public DateTime Deadline { get; set; }
		public int TotalQuestions { get; set; }
		public int AnsweredCount { get; set; }
		public bool Resumed { get; set; }
		public List<QuestionDTO> Questions { get; set; } = new();
	}
}
