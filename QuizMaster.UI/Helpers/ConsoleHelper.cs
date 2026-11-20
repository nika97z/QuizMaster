using System.Globalization;
using System.Text;

namespace QuizMaster.UI.Helpers
{
	/// <summary>
	/// Console input and output helpers. Every read validates its input and asks again, so bad input never crashes the app.
	/// </summary>
	public static class ConsoleHelper
	{
		public static void Header(string title)
		{
			Console.WriteLine();
			WriteColored($"=== {title} ===", ConsoleColor.Cyan);
		}

		public static void Info(string message) => WriteColored(message, ConsoleColor.Gray);
		public static void Success(string message) => WriteColored(message, ConsoleColor.Green);
		public static void Warning(string message) => WriteColored(message, ConsoleColor.Yellow);
		public static void Error(string message) => WriteColored(message, ConsoleColor.Red);

		public static void WriteColored(string message, ConsoleColor color)
		{
			var previous = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(message);
			Console.ForegroundColor = previous;
		}

		public static string ReadLine(string prompt)
		{
			Console.Write($"{prompt}: ");
			var line = Console.ReadLine() ?? throw new EndOfInputException();
			if (Console.IsInputRedirected)
			{
				// Echo piped input so a transcript of the session stays readable.
				Console.WriteLine(line);
			}
			return line.Trim();
		}

		public static string ReadOptional(string prompt)
		{
			return ReadLine(prompt);
		}

		public static string ReadNonEmpty(string prompt, int maxLength = 1000)
		{
			while (true)
			{
				var value = ReadLine(prompt);
				if (value.Length == 0)
				{
					Warning("This can't be empty.");
				}
				else if (value.Length > maxLength)
				{
					Warning($"Use at most {maxLength} characters.");
				}
				else
				{
					return value;
				}
			}
		}

		public static int ReadInt(string prompt, int min, int max)
		{
			var range = max == int.MaxValue ? string.Empty : $" ({min}-{max})";
			while (true)
			{
				var text = ReadLine($"{prompt}{range}");
				if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= min && value <= max)
				{
					return value;
				}
				Warning(max == int.MaxValue ? $"Enter a whole number of at least {min}." : $"Enter a whole number from {min} to {max}.");
			}
		}

		public static int? ReadOptionalInt(string prompt, int min, int max)
		{
			while (true)
			{
				var text = ReadLine($"{prompt} ({min}-{max}, Enter = keep)");
				if (text.Length == 0)
				{
					return null;
				}
				if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= min && value <= max)
				{
					return value;
				}
				Warning($"Enter a whole number from {min} to {max}, or press Enter to keep the current value.");
			}
		}

		public static bool Confirm(string prompt)
		{
			while (true)
			{
				var text = ReadLine($"{prompt}? (y/n)").ToLowerInvariant();
				if (text is "y" or "yes")
				{
					return true;
				}
				if (text is "n" or "no")
				{
					return false;
				}
				Warning("Please answer y or n.");
			}
		}

		public static bool ReadTrueFalse(string prompt)
		{
			while (true)
			{
				var text = ReadLine(prompt).ToLowerInvariant();
				if (text is "t" or "true")
				{
					return true;
				}
				if (text is "f" or "false")
				{
					return false;
				}
				Warning("Please answer t (true) or f (false).");
			}
		}

		/// <summary>
		/// Reads a password without showing it (each key is shown as *).
		/// </summary>
		public static string ReadPassword(string prompt)
		{
			Console.Write($"{prompt}: ");
			if (Console.IsInputRedirected)
			{
				var line = Console.ReadLine() ?? throw new EndOfInputException();
				Console.WriteLine(new string('*', line.Length));
				return line;
			}

			var builder = new StringBuilder();
			while (true)
			{
				var key = Console.ReadKey(intercept: true);
				if (key.Key == ConsoleKey.Enter)
				{
					Console.WriteLine();
					return builder.ToString();
				}
				if (key.Key == ConsoleKey.Backspace)
				{
					if (builder.Length > 0)
					{
						builder.Length--;
						Console.Write("\b \b");
					}
					continue;
				}
				if (!char.IsControl(key.KeyChar))
				{
					builder.Append(key.KeyChar);
					Console.Write('*');
				}
			}
		}

		/// <summary>
		/// Parses "1,3" or "1 3" into numbers. Returns null when the text isn't a list of numbers within the range.
		/// </summary>
		public static List<int>? ParseNumbers(string text, int min, int max)
		{
			var parts = text.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0)
			{
				return null;
			}

			var numbers = new List<int>();
			foreach (var part in parts)
			{
				if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) || number < min || number > max)
				{
					return null;
				}
				numbers.Add(number);
			}
			return numbers;
		}

		public static void PrintTable(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows, int maxColumnWidth = 40)
		{
			var data = rows.Select(r => r.Select(c => Truncate(c ?? string.Empty, maxColumnWidth)).ToList()).ToList();
			var widths = headers
				.Select((h, i) => Math.Max(h.Length, data.Count == 0 ? 0 : data.Max(r => i < r.Count ? r[i].Length : 0)))
				.ToList();

			string Format(IReadOnlyList<string> cells) =>
				string.Join(" | ", widths.Select((w, i) => (i < cells.Count ? cells[i] : string.Empty).PadRight(w))).TrimEnd();

			Console.WriteLine(Format(headers));
			Console.WriteLine(string.Join("-+-", widths.Select(w => new string('-', w))));
			foreach (var row in data)
			{
				Console.WriteLine(Format(row));
			}
			if (data.Count == 0)
			{
				Info("(nothing to show)");
			}
		}

		public static string Truncate(string text, int max)
		{
			return text.Length <= max ? text : text[..(max - 3)] + "...";
		}

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
	}
}
