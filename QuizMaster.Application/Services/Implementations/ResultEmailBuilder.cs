using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using System.Globalization;
using System.Text;

namespace QuizMaster.Application.Services.Implementations
{
	/// <summary>
	/// Builds the plain-text result email, including a text certificate when the student passed.
	/// </summary>
	internal static class ResultEmailBuilder
	{
		public static string BuildSubject(Attempt attempt)
		{
			return $"QuizMaster result: {attempt.Quiz.Title} - {attempt.Result}";
		}

		public static string BuildBody(Attempt attempt)
		{
			var percent = attempt.Percent.ToString("0.##", CultureInfo.InvariantCulture);
			var builder = new StringBuilder();
			builder.AppendLine($"Hello {attempt.Student.Username},");
			builder.AppendLine();
			builder.AppendLine($"You finished the quiz \"{attempt.Quiz.Title}\".");
			builder.AppendLine();
			builder.AppendLine($"Score:   {attempt.Score} / {attempt.MaxScore}");
			builder.AppendLine($"Percent: {percent}%");
			builder.AppendLine($"Result:  {attempt.Result} (pass mark {attempt.Quiz.PassPercent}%)");

			if (attempt.Result == AttemptResult.Passed && attempt.CertificateCode != null)
			{
				builder.AppendLine();
				builder.AppendLine("==============================================");
				builder.AppendLine("          CERTIFICATE OF COMPLETION");
				builder.AppendLine("==============================================");
				builder.AppendLine($"This certifies that {attempt.Student.Username}");
				builder.AppendLine($"passed \"{attempt.Quiz.Title}\" with {percent}%.");
				builder.AppendLine($"Date: {attempt.FinishedAt:yyyy-MM-dd}");
				builder.AppendLine($"Certificate code: {attempt.CertificateCode}");
				builder.AppendLine("==============================================");
			}
			else
			{
				builder.AppendLine();
				builder.AppendLine("You can take the quiz again to improve your result.");
			}

			builder.AppendLine();
			builder.AppendLine("QuizMaster");
			return builder.ToString();
		}
	}
}
