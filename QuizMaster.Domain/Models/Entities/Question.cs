using QuizMaster.Domain.Models.Enums;
using System.Text;

namespace QuizMaster.Domain.Models.Entities
{
	/// <summary>
	/// Base class of the Question → SingleChoice / MultiChoice / TrueFalse hierarchy (one Questions table, TPH).
	/// The correct answer lives in a private field of each subclass and can only be checked through CheckAnswer().
	/// </summary>
	public abstract class Question
	{
		public const int MaxOptions = 10;

		// Column size in the database (see QuestionConfiguration). Validation doesn't check it;
		// the input forms and the AI quiz generator keep texts within it.
		public const int MaxTextLength = 1000;

		public int Id { get; set; }
		public int QuizId { get; set; }
		public Quiz Quiz { get; set; } = null!;
		public string Text { get; protected set; } = string.Empty;
		public int Points { get; protected set; }
		public int OrderIndex { get; set; }
		public List<QuestionOption> Options { get; protected set; } = new();

		/// <summary>
		/// Not a column: the Type column is the TPH discriminator.
		/// </summary>
		public abstract QuestionType Type { get; }

		protected Question()
		{
		}

		protected Question(string text, int points)
		{
			SetBasics(text, points);
		}

		/// <summary>
		/// Returns the question as text: a type-specific instruction plus numbered options in the given order
		/// (a list of option positions). Never writes to the console; the UI prints the result.
		/// </summary>
		public abstract string DisplayQuestion(IReadOnlyList<int>? optionOrder = null);

		/// <summary>
		/// Checks an answer. The indexes are ORIGINAL option positions (0-based), not display numbers.
		/// </summary>
		public abstract bool CheckAnswer(IReadOnlyCollection<int> selectedOptionIndexes);

		/// <summary>
		/// Returns every problem with this question. An empty list means the question is valid.
		/// </summary>
		public abstract IReadOnlyList<string> Validate();

		/// <summary>
		/// Checks that a selection is acceptable for this question type. Returns an error message, or null when it is fine.
		/// </summary>
		public virtual string? ValidateSelection(IReadOnlyCollection<int>? selectedOptionIndexes)
		{
			if (selectedOptionIndexes == null || selectedOptionIndexes.Count == 0)
			{
				return "Choose an answer.";
			}
			if (selectedOptionIndexes.Any(i => i < 0 || i >= Options.Count))
			{
				return $"Each answer must be a number from 1 to {Options.Count}.";
			}
			return null;
		}

		protected void SetBasics(string text, int points)
		{
			Text = text?.Trim() ?? string.Empty;
			Points = points;
		}

		protected void SetOptions(IEnumerable<string>? optionTexts)
		{
			Options.Clear();
			var position = 0;
			foreach (var optionText in optionTexts ?? Enumerable.Empty<string>())
			{
				Options.Add(new QuestionOption { Position = position++, Text = optionText?.Trim() ?? string.Empty });
			}
		}

		protected List<string> ValidateCommon()
		{
			var errors = new List<string>();
			if (string.IsNullOrWhiteSpace(Text))
			{
				errors.Add("Question text can't be empty.");
			}
			if (Points <= 0)
			{
				errors.Add("Points must be a positive number.");
			}
			return errors;
		}

		protected List<string> ValidateOptions(int minCount)
		{
			var errors = new List<string>();
			if (Options.Count < minCount)
			{
				errors.Add($"The question needs at least {minCount} options.");
			}
			if (Options.Count > MaxOptions)
			{
				errors.Add($"The question can have at most {MaxOptions} options.");
			}
			if (Options.Any(o => string.IsNullOrWhiteSpace(o.Text)))
			{
				errors.Add("Options can't be empty.");
			}
			var hasDuplicates = Options
				.Where(o => !string.IsNullOrWhiteSpace(o.Text))
				.GroupBy(o => o.Text.Trim(), StringComparer.OrdinalIgnoreCase)
				.Any(g => g.Count() > 1);
			if (hasDuplicates)
			{
				errors.Add("Options must be different from each other.");
			}
			return errors;
		}

		protected string RenderOptions(IReadOnlyList<int>? optionOrder)
		{
			var ordered = Options.OrderBy(o => o.Position).ToList();
			var order = IsPermutation(optionOrder, ordered.Count)
				? optionOrder!
				: ordered.Select(o => o.Position).ToList();

			var builder = new StringBuilder();
			var number = 1;
			foreach (var position in order)
			{
				var option = ordered.FirstOrDefault(o => o.Position == position);
				if (option != null)
				{
					builder.AppendLine($"  {number++}) {option.Text}");
				}
			}
			return builder.ToString().TrimEnd();
		}

		protected string PointsLabel => Points == 1 ? "1 point" : $"{Points} points";

		private static bool IsPermutation(IReadOnlyList<int>? order, int count)
		{
			return order != null
				&& order.Count == count
				&& order.OrderBy(i => i).SequenceEqual(Enumerable.Range(0, count));
		}
	}
}
