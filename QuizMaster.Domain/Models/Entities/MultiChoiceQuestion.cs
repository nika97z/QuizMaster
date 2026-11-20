using QuizMaster.Domain.Helpers;
using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.Entities
{
	public class MultiChoiceQuestion : Question
	{
		public const string AtLeastOneCorrectMessage = "A multiple choice question must have at least one correct option.";

		// Encapsulation: the correct answers are private, stored as "0;2". EF Core maps this field to CorrectOptionIndexes.
		private string _correctOptionIndexes = string.Empty;

		private MultiChoiceQuestion()
		{
		}

		public MultiChoiceQuestion(string text, int points, IEnumerable<string> options, IEnumerable<int> correctOptionIndexes)
			: base(text, points)
		{
			SetOptions(options);
			SetCorrectOptions(correctOptionIndexes);
		}

		public override QuestionType Type => QuestionType.Multi;

		public void Update(string text, int points, IEnumerable<string> options, IEnumerable<int> correctOptionIndexes)
		{
			SetBasics(text, points);
			SetOptions(options);
			SetCorrectOptions(correctOptionIndexes);
		}

		public void SetCorrectOptions(IEnumerable<int> correctOptionIndexes)
		{
			_correctOptionIndexes = IndexList.JoinDistinctSorted(correctOptionIndexes ?? Enumerable.Empty<int>());
		}

		public override string DisplayQuestion(IReadOnlyList<int>? optionOrder = null)
		{
			return $"{Text}  ({PointsLabel})\nChoose ALL correct answers (e.g. 1,3):\n{RenderOptions(optionOrder)}";
		}

		/// <summary>
		/// Correct only when the chosen set equals the correct set exactly. Repeated choices count once.
		/// </summary>
		public override bool CheckAnswer(IReadOnlyCollection<int> selectedOptionIndexes)
		{
			if (selectedOptionIndexes == null
				|| !IndexList.TryParse(_correctOptionIndexes, out var correct)
				|| correct.Count == 0)
			{
				return false;
			}
			var chosen = selectedOptionIndexes.Distinct().OrderBy(i => i);
			return chosen.SequenceEqual(correct.Distinct().OrderBy(i => i));
		}

		public override IReadOnlyList<string> Validate()
		{
			var errors = ValidateCommon();
			errors.AddRange(ValidateOptions(minCount: 2));
			if (!IndexList.TryParse(_correctOptionIndexes, out var correct))
			{
				errors.Add("The stored correct answer is corrupted.");
			}
			else if (correct.Count == 0)
			{
				errors.Add(AtLeastOneCorrectMessage);
			}
			else if (correct.Any(i => i < 0 || i >= Options.Count))
			{
				errors.Add("Every correct option must be one of the question's options.");
			}
			return errors;
		}
	}
}
