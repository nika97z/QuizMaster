namespace QuizMaster.Domain.Models.DTOs.QuizDTOs
{
	public class QuizDTO
	{
		public int Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Topic { get; set; } = string.Empty;
		public int QuestionCount { get; set; }
		public int MaxScore { get; set; }
		public int TimeLimitMinutes { get; set; }
		public int PassPercent { get; set; }
		public bool IsPublished { get; set; }
		public string LecturerName { get; set; } = string.Empty;

		public override string ToString()
		{
			return $"{Id} {Title} ({Topic}) {QuestionCount} questions, max {MaxScore}, {TimeLimitMinutes} min, pass {PassPercent}%";
		}
	}
}
