using QuizMaster.Domain.Models.DTOs.AttemptDTOs;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.UI.Helpers;

namespace QuizMaster.UI.Menus
{
	public static class ResultView
	{
		public static void Show(AttemptResultDTO result, bool justFinished)
		{
			ConsoleHelper.Header($"Result - {result.QuizTitle}");
			Console.WriteLine($"Score:     {result.Score} / {result.MaxScore}");
			Console.WriteLine($"Percent:   {ConsoleHelper.FormatPercent(result.Percent)} (pass mark {result.PassPercent}%)");
			Console.WriteLine($"Correct:   {result.CorrectCount}    Incorrect: {result.IncorrectCount}    (of {result.TotalQuestions})");

			if (result.Result == AttemptResult.Passed)
			{
				ConsoleHelper.Success("Result:    PASSED");
				if (result.CertificateCode != null)
				{
					ConsoleHelper.Success($"Certificate code: {result.CertificateCode}");
				}
			}
			else
			{
				ConsoleHelper.Error("Result:    FAILED");
			}

			if (result.TimedOut)
			{
				ConsoleHelper.Warning("The time limit was reached.");
			}
			if (justFinished)
			{
				if (result.EmailSent)
				{
					ConsoleHelper.Info("Your result was emailed to you.");
				}
				else
				{
					ConsoleHelper.Warning("The result email couldn't be sent. The problem was logged.");
				}
			}

			Console.WriteLine();
			ConsoleHelper.PrintTable(
				new[] { "#", "Question", "Your answer", "Result", "Points" },
				result.Review.Select(r => new[]
				{
					r.Number.ToString(), r.QuestionText, r.YourAnswer, r.IsCorrect ? "Correct" : "Incorrect", $"{r.PointsAwarded}/{r.Points}"
				}));
		}
	}
}
