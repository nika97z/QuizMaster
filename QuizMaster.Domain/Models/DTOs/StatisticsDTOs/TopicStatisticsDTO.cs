namespace QuizMaster.Domain.Models.DTOs.StatisticsDTOs
{
	public class TopicStatisticsDTO
	{
		public string Topic { get; set; } = string.Empty;
		public int QuizCount { get; set; }
		public int AttemptCount { get; set; }
		public decimal AveragePercent { get; set; }
		public decimal PassRate { get; set; }
	}
}
