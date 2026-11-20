namespace QuizMaster.Domain.Models.DTOs.StatisticsDTOs
{
	public class QuizStatisticsDTO
	{
		public int QuizId { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Topic { get; set; } = string.Empty;
		public int AttemptCount { get; set; }
		public decimal AverageScore { get; set; }
		public decimal AveragePercent { get; set; }
		public decimal PassRate { get; set; }
		public string BestStudent { get; set; } = string.Empty;
	}
}
