using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.Entities
{
	public class TrueFalseQuestion : Question
	{
		public const int TrueIndex = 0;
		public const int FalseIndex = 1;
		public const string OneAnswerMessage = "A true/false question must have exactly one answer: true or false.";

		private static readonly string[] FixedOptions = { "True", "False" };

		// Encapsulation: the correct answer is private. EF Core maps this field to the CorrectValue column.
		private bool _correctValue;

		private TrueFalseQuestion()
		{
		}

		public TrueFalseQuestion(string text, int points, bool correctValue)
			: base(text, points)
		{
			SetOptions(FixedOptions);
			_correctValue = correctValue;
		}

		public override QuestionType Type => QuestionType.TrueFalse;

		public void Update(string text, int points, bool correctValue)
		{
			SetBasics(text, points);
			if (!HasFixedOptions())
			{
				SetOptions(FixedOptions);
			}
			SetCorrectValue(correctValue);
		}

		public void SetCorrectValue(bool correctValue)
		{
			_correctValue = correctValue;
		}

		public override string DisplayQuestion(IReadOnlyList<int>? optionOrder = null)
		{
			return $"{Text}  ({PointsLabel})\nTrue or false?\n{RenderOptions(optionOrder)}";
		}

		public override bool CheckAnswer(IReadOnlyCollection<int> selectedOptionIndexes)
		{
			if (selectedOptionIndexes == null)
			{
				return false;
			}
			var chosen = selectedOptionIndexes.Distinct().ToList();
			if (chosen.Count != 1)
			{
				return false;
			}
			return chosen[0] switch
			{
				TrueIndex => _correctValue,
				FalseIndex => !_correctValue,
				_ => false
			};
		}

		public override string? ValidateSelection(IReadOnlyCollection<int>? selectedOptionIndexes)
		{
			var error = base.ValidateSelection(selectedOptionIndexes);
			if (error != null)
			{
				return error;
			}
			return selectedOptionIndexes!.Distinct().Count() == 1 ? null : "Choose exactly one answer: True or False.";
		}

		public override IReadOnlyList<string> Validate()
		{
			var errors = ValidateCommon();
			if (!HasFixedOptions())
			{
				errors.Add("A true/false question must have exactly the options True and False.");
			}
			return errors;
		}

		private bool HasFixedOptions()
		{
			return Options.Count == 2
				&& Options.OrderBy(o => o.Position)
					.Select(o => o.Text)
					.SequenceEqual(FixedOptions, StringComparer.OrdinalIgnoreCase);
		}
	}
}
