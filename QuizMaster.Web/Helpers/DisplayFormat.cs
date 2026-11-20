using QuizMaster.Domain.Models.Enums;
using System.Globalization;

namespace QuizMaster.Web.Helpers
{
	/// <summary>
	/// Formats percents, dates and durations like the console's ConsoleHelper. The web project doesn't reference
	/// QuizMaster.UI, so these are small copies, plus a few labels for the pages.
	/// </summary>
	public static class DisplayFormat
	{
		public static string FormatPercent(decimal value)
		{
			return value.ToString("0.##", CultureInfo.InvariantCulture) + "%";
		}

		public static string FormatDate(DateTime? value)
		{
			return value?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "-";
		}

		public static string FormatDuration(TimeSpan span)
		{
			if (span < TimeSpan.Zero)
			{
				span = TimeSpan.Zero;
			}
			return span.TotalHours >= 1
				? span.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
				: span.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Log entries show seconds too, like the console's log view.
		/// </summary>
		public static string FormatTimestamp(DateTime value)
		{
			return value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}

		public static string FormatPoints(int points)
		{
			return points == 1 ? "1 point" : $"{points} points";
		}

		public static string FormatType(QuestionType type)
		{
			return type switch
			{
				QuestionType.Single => "Single choice",
				QuestionType.Multi => "Multiple choice",
				QuestionType.TrueFalse => "True/False",
				_ => type.ToString()
			};
		}

		/// <summary>
		/// The instruction shown above the options, as in each question type's DisplayQuestion().
		/// </summary>
		public static string FormatInstruction(QuestionType type)
		{
			return type switch
			{
				QuestionType.Multi => "Choose ALL correct answers.",
				QuestionType.TrueFalse => "True or false?",
				_ => "Choose ONE answer."
			};
		}

		public static string FormatQuizStatus(bool isPublished)
		{
			return isPublished ? "Published" : "Draft/closed";
		}

		public static string FormatResult(AttemptResult result)
		{
			return result == AttemptResult.None ? "-" : result.ToString();
		}

		public static string Truncate(string text, int max)
		{
			return text.Length <= max ? text : text[..(max - 3)] + "...";
		}
	}
}
