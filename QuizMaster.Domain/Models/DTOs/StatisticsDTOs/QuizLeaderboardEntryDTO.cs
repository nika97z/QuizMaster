namespace QuizMaster.Domain.Models.DTOs.StatisticsDTOs
{
	public class QuizLeaderboardEntryDTO
	{
		public int Rank { get; set; }
		public string Username { get; set; } = string.Empty;
		public int Score { get; set; }
		public int MaxScore { get; set; }
		public decimal Percent { get; set; }
		public TimeSpan Duration { get; set; }
		public DateTime? FinishedAt { get; set; }
	}
}
