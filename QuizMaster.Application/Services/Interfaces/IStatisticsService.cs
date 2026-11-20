using QuizMaster.Domain.Models.DTOs.StatisticsDTOs;

namespace QuizMaster.Application.Services.Interfaces
{
	public interface IStatisticsService
	{
		List<LeaderboardEntryDTO> GetOverallLeaderboard();
		List<QuizLeaderboardEntryDTO> GetQuizLeaderboard(int quizId);
		List<QuizStatisticsDTO> GetQuizStatistics();
		List<TopicStatisticsDTO> GetTopicStatistics();
		LeaderboardEntryDTO? GetBestStudent();
		decimal GetAverageScore(int? quizId = null);
		decimal GetAveragePercent(int? quizId = null);
	}
}
