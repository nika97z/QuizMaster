namespace QuizMaster.Domain.Models.DTOs.QuizDTOs
{
	public class UpdateQuizDTO
	{
		public int Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Topic { get; set; } = string.Empty;
		public int TimeLimitMinutes { get; set; }
		public int PassPercent { get; set; }
	}
}
