using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Domain.Models.DTOs.StatisticsDTOs;
using QuizMaster.UI.Helpers;

namespace QuizMaster.UI.Menus
{
	public static class LeaderboardViews
	{
		public static void ShowOverall(ServiceRunner services)
		{
			var entries = services.Get<IStatisticsService, List<LeaderboardEntryDTO>>(s => s.GetOverallLeaderboard());
			ConsoleHelper.Header("Overall leaderboard (best score per quiz)");
			ConsoleHelper.PrintTable(
				new[] { "Rank", "Student", "Total best score", "Average %", "Quizzes", "Attempts" },
				entries.Select(e => new[]
				{
					e.Rank.ToString(), e.Username, e.TotalBestScore.ToString(), ConsoleHelper.FormatPercent(e.AveragePercent),
					e.QuizzesTaken.ToString(), e.AttemptCount.ToString()
				}));
		}

		public static void ChooseQuizAndShow(ServiceRunner services, List<QuizDTO> quizzes)
		{
			if (quizzes.Count == 0)
			{
				ConsoleHelper.Info("There are no quizzes yet.");
				return;
			}

			ConsoleHelper.Header("Choose a quiz");
			ConsoleHelper.PrintTable(new[] { "Id", "Title", "Topic" }, quizzes.Select(q => new[] { q.Id.ToString(), q.Title, q.Topic }));
			var quizId = ConsoleHelper.ReadInt("Quiz id (0 = back)", 0, int.MaxValue);
			if (quizId == 0)
			{
				return;
			}
			var quiz = quizzes.FirstOrDefault(q => q.Id == quizId);
			if (quiz == null)
			{
				ConsoleHelper.Error("There is no quiz with that id.");
				return;
			}

			var entries = services.Get<IStatisticsService, List<QuizLeaderboardEntryDTO>>(s => s.GetQuizLeaderboard(quiz.Id));
			ConsoleHelper.Header($"Leaderboard - {quiz.Title}");
			ConsoleHelper.PrintTable(
				new[] { "Rank", "Student", "Score", "Percent", "Time", "Finished" },
				entries.Select(e => new[]
				{
					e.Rank.ToString(), e.Username, $"{e.Score}/{e.MaxScore}", ConsoleHelper.FormatPercent(e.Percent),
					ConsoleHelper.FormatDuration(e.Duration), ConsoleHelper.FormatDate(e.FinishedAt)
				}));
		}
	}
}
