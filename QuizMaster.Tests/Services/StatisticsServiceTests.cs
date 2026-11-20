using Moq;
using QuizMaster.Application.Services.Implementations;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Tests.TestHelpers;

namespace QuizMaster.Tests.Services
{
	public class StatisticsServiceTests
	{
		private readonly Mock<IAttemptRepository> _attemptRepository = new();
		private readonly StatisticsService _statisticsService;

		public StatisticsServiceTests()
		{
			var lecturer = TestData.Lecturer();
			var programming = TestData.PublishedQuiz(10, lecturer, topic: "Programming");
			var databases = TestData.PublishedQuiz(20, lecturer, topic: "Databases");
			var luka = TestData.Student(2, "luka");
			var nino = TestData.Student(3, "nino");
			var ana = TestData.Student(4, "ana");
			var start = TestData.Now;

			var cancelled = TestData.StartedAttempt(databases, luka, start);
			cancelled.Cancel(start.AddMinutes(1));

			// Pass mark is 50% for every quiz.
			_attemptRepository.Setup(r => r.GetFinishedAttempts()).Returns(new List<Attempt>
			{
				TestData.FinishedAttempt(programming, luka, 1, start),              // 2/6  = 33.33%  Failed
				TestData.FinishedAttempt(programming, luka, 3, start.AddDays(1)),   // 6/6  = 100%    Passed
				TestData.FinishedAttempt(programming, nino, 2, start, minutes: 8),  // 5/6  = 83.33%  Passed
				TestData.FinishedAttempt(databases, nino, 3, start),                // 6/6  = 100%    Passed
				TestData.FinishedAttempt(databases, ana, 0, start),                 // 0/6  = 0%      Failed
				cancelled                                                            // must be ignored
			});
			_statisticsService = new StatisticsService(_attemptRepository.Object);
		}

		[Fact]
		public void OverallLeaderboard_IsSortedByTotalBestScore()
		{
			var board = _statisticsService.GetOverallLeaderboard();

			Assert.Equal(new[] { "nino", "luka", "ana" }, board.Select(e => e.Username));
			Assert.Equal(new[] { 1, 2, 3 }, board.Select(e => e.Rank));
			Assert.Equal(11, board[0].TotalBestScore);
			Assert.Equal(6, board[1].TotalBestScore);
			Assert.Equal(2, board[1].AttemptCount);
		}

		[Fact]
		public void OverallLeaderboard_IgnoresCancelledAttempts()
		{
			var luka = _statisticsService.GetOverallLeaderboard().Single(e => e.Username == "luka");

			Assert.Equal(1, luka.QuizzesTaken);
		}

		[Fact]
		public void BestStudent_IsTheLeaderboardWinner()
		{
			Assert.Equal("nino", _statisticsService.GetBestStudent()?.Username);
		}

		[Fact]
		public void QuizLeaderboard_KeepsEachStudentsBestAttempt()
		{
			var board = _statisticsService.GetQuizLeaderboard(10);

			Assert.Equal(2, board.Count);
			Assert.Equal("luka", board[0].Username);
			Assert.Equal(100m, board[0].Percent);
			Assert.Equal("nino", board[1].Username);
			Assert.Equal(TimeSpan.FromMinutes(8), board[1].Duration);
		}

		[Fact]
		public void Averages_UseAverageOverFinishedAttempts()
		{
			Assert.Equal(4.33m, _statisticsService.GetAverageScore(10));
			Assert.Equal(72.22m, _statisticsService.GetAveragePercent(10));
			Assert.Equal(3.8m, _statisticsService.GetAverageScore());
		}

		[Fact]
		public void TopicStatistics_AreGroupedByTopic()
		{
			var topics = _statisticsService.GetTopicStatistics();

			Assert.Equal(new[] { "Databases", "Programming" }, topics.Select(t => t.Topic));
			Assert.Equal(2, topics[0].AttemptCount);
			Assert.Equal(50m, topics[0].AveragePercent);
			Assert.Equal(50m, topics[0].PassRate);
			Assert.Equal(3, topics[1].AttemptCount);
		}

		[Fact]
		public void QuizStatistics_AreGroupedByQuiz()
		{
			var stats = _statisticsService.GetQuizStatistics();

			var programming = stats.Single(s => s.QuizId == 10);
			Assert.Equal(3, programming.AttemptCount);
			Assert.Equal(66.67m, programming.PassRate);
			Assert.Equal("luka", programming.BestStudent);
			Assert.Equal("nino", stats.Single(s => s.QuizId == 20).BestStudent);
		}
	}
}
