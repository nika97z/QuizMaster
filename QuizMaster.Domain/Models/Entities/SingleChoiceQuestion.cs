using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.Entities
{
	public class SingleChoiceQuestion : Question
	{
		public const string ExactlyOneCorrectMessage = "A single choice question must have exactly one correct option.";

		// Encapsulation: the correct answer is private. EF Core maps this field to the CorrectOptionIndex column.
		private int _correctOptionIndex = -1;

		private SingleChoiceQuestion()
		{
		}

		public SingleChoiceQuestion(string text, int points, IEnumerable<string> options, int correctOptionIndex)
			: base(text, points)
		{
			SetOptions(options);
			_correctOptionIndex = correctOptionIndex;
		}

		public override QuestionType Type => QuestionType.Single;

		public void Update(string text, int points, IEnumerable<string> options, int correctOptionIndex)
		{
			SetBasics(text, points);
			SetOptions(options);
			SetCorrectOption(correctOptionIndex);
		}

		public void SetCorrectOption(int correctOptionIndex)
		{
			_correctOptionIndex = correctOptionIndex;
		}

		public override string DisplayQuestion(IReadOnlyList<int>? optionOrder = null)
		{
			return $"{Text}  ({PointsLabel})\nChoose ONE answer:\n{RenderOptions(optionOrder)}";
		}

		public override bool CheckAnswer(IReadOnlyCollection<int> selectedOptionIndexes)
		{
			if (selectedOptionIndexes == null)
			{
				return false;
			}
			var chosen = selectedOptionIndexes.Distinct().ToList();
			return chosen.Count == 1 && chosen[0] == _correctOptionIndex;
		}

		public override string? ValidateSelection(IReadOnlyCollection<int>? selectedOptionIndexes)
		{
			var error = base.ValidateSelection(selectedOptionIndexes);
			if (error != null)
			{
				return error;
			}
			return selectedOptionIndexes!.Distinct().Count() == 1 ? null : "Choose exactly one answer.";
		}

		public override IReadOnlyList<string> Validate()
		{
			var errors = ValidateCommon();
			errors.AddRange(ValidateOptions(minCount: 2));
			if (_correctOptionIndex < 0 || _correctOptionIndex >= Options.Count)
			{
				errors.Add(ExactlyOneCorrectMessage);
			}
			return errors;
		}
	}
}
