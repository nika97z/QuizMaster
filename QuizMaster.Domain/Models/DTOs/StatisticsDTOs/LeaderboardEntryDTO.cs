namespace QuizMaster.Domain.Models.DTOs.StatisticsDTOs
{
	public class LeaderboardEntryDTO
	{
		public int Rank { get; set; }
		public int StudentId { get; set; }
		public string Username { get; set; } = string.Empty;
		public int TotalBestScore { get; set; }
		public decimal AveragePercent { get; set; }
		public int QuizzesTaken { get; set; }
		public int AttemptCount { get; set; }
	}
}
