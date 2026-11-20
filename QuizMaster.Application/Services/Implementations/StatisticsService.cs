using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.StatisticsDTOs;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Application.Services.Implementations
{
	/// <summary>
	/// Leaderboards and statistics, built with LINQ (GroupBy, OrderByDescending, Average, ...) over finished attempts only.
	/// </summary>
	public class StatisticsService : IStatisticsService
	{
		private readonly IAttemptRepository _attemptRepository;

		public StatisticsService(IAttemptRepository attemptRepository)
		{
			_attemptRepository = attemptRepository;
		}

		public List<LeaderboardEntryDTO> GetOverallLeaderboard()
		{
			var entries = FinishedAttempts()
				.GroupBy(a => a.StudentId)
				.Select(studentAttempts =>
				{
					// Best attempt per quiz, so retaking a quiz can only improve a student's total.
					var bestPerQuiz = studentAttempts
						.GroupBy(a => a.QuizId)
						.Select(quizAttempts => quizAttempts
							.OrderByDescending(a => a.Percent)
							.ThenByDescending(a => a.Score)
							.First())
						.ToList();

					return new LeaderboardEntryDTO
					{
						StudentId = studentAttempts.Key,
						Username = studentAttempts.First().Student.Username,
						TotalBestScore = bestPerQuiz.Sum(a => a.Score),
						AveragePercent = Round(bestPerQuiz.Average(a => a.Percent)),
						QuizzesTaken = bestPerQuiz.Count,
						AttemptCount = studentAttempts.Count()
					};
				})
				.OrderByDescending(e => e.TotalBestScore)
				.ThenByDescending(e => e.AveragePercent)
				.ThenBy(e => e.Username)
				.ToList();

			for (var i = 0; i < entries.Count; i++)
			{
				entries[i].Rank = i + 1;
			}
			return entries;
		}

		public List<QuizLeaderboardEntryDTO> GetQuizLeaderboard(int quizId)
		{
			return FinishedAttempts(quizId)
				.GroupBy(a => a.StudentId)
				.Select(studentAttempts => studentAttempts
					.OrderByDescending(a => a.Percent)
					.ThenBy(a => Duration(a))
					.First())
				.OrderByDescending(a => a.Percent)
				.ThenBy(a => Duration(a))
				.Select((a, index) => new QuizLeaderboardEntryDTO
				{
					Rank = index + 1,
					Username = a.Student.Username,
					Score = a.Score,
					MaxScore = a.MaxScore,
					Percent = a.Percent,
					Duration = Duration(a),
					FinishedAt = a.FinishedAt
				})
				.ToList();
		}

		public List<QuizStatisticsDTO> GetQuizStatistics()
		{
			return FinishedAttempts()
				.GroupBy(a => a.QuizId)
				.Select(quizAttempts => new QuizStatisticsDTO
				{
					QuizId = quizAttempts.Key,
					Title = quizAttempts.First().Quiz.Title,
					Topic = quizAttempts.First().Quiz.Topic,
					AttemptCount = quizAttempts.Count(),
					AverageScore = Round(quizAttempts.Average(a => (decimal)a.Score)),
					AveragePercent = Round(quizAttempts.Average(a => a.Percent)),
					PassRate = Round(quizAttempts.Count(a => a.Result == AttemptResult.Passed) * 100m / quizAttempts.Count()),
					BestStudent = quizAttempts
						.OrderByDescending(a => a.Percent)
						.ThenBy(a => Duration(a))
						.First().Student.Username
				})
				.OrderByDescending(s => s.AveragePercent)
				.ThenBy(s => s.Title)
				.ToList();
		}

		public List<TopicStatisticsDTO> GetTopicStatistics()
		{
			return FinishedAttempts()
				.GroupBy(a => a.Quiz.Topic.Trim(), StringComparer.OrdinalIgnoreCase)
				.Select(topicAttempts => new TopicStatisticsDTO
				{
					Topic = topicAttempts.Key,
					QuizCount = topicAttempts.Select(a => a.QuizId).Distinct().Count(),
					AttemptCount = topicAttempts.Count(),
					AveragePercent = Round(topicAttempts.Average(a => a.Percent)),
					PassRate = Round(topicAttempts.Count(a => a.Result == AttemptResult.Passed) * 100m / topicAttempts.Count())
				})
				.OrderBy(t => t.Topic)
				.ToList();
		}

		public LeaderboardEntryDTO? GetBestStudent()
		{
			return GetOverallLeaderboard().FirstOrDefault();
		}

		public decimal GetAverageScore(int? quizId = null)
		{
			var attempts = FinishedAttempts(quizId);
			return attempts.Count == 0 ? 0m : Round(attempts.Average(a => (decimal)a.Score));
		}

		public decimal GetAveragePercent(int? quizId = null)
		{
			var attempts = FinishedAttempts(quizId);
			return attempts.Count == 0 ? 0m : Round(attempts.Average(a => a.Percent));
		}

		private List<Attempt> FinishedAttempts(int? quizId = null)
		{
			return _attemptRepository.GetFinishedAttempts()
				.Where(a => a.Status == AttemptStatus.Finished)
				.Where(a => !quizId.HasValue || a.QuizId == quizId.Value)
				.ToList();
		}

		private static TimeSpan Duration(Attempt attempt)
		{
			return (attempt.FinishedAt ?? attempt.StartedAt) - attempt.StartedAt;
		}

		private static decimal Round(decimal value)
		{
			return Math.Round(value, 2, MidpointRounding.AwayFromZero);
		}
	}
}
