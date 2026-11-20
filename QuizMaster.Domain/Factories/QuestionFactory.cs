using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Factories
{
	/// <summary>
	/// Builds and updates questions of the right subclass. Only valid questions come out:
	/// every problem is collected and thrown together as a DomainValidationException.
	/// </summary>
	public static class QuestionFactory
	{
		public static Question Create(
			QuestionType type,
			string text,
			int points,
			IReadOnlyCollection<string>? options,
			IReadOnlyCollection<int>? correctOptionIndexes)
		{
			var correct = Distinct(correctOptionIndexes);
			var errors = new List<string>();
			Question question;

			switch (type)
			{
				case QuestionType.Single:
					if (correct.Count != 1)
					{
						errors.Add(SingleChoiceQuestion.ExactlyOneCorrectMessage);
					}
					question = new SingleChoiceQuestion(text, points, options ?? Array.Empty<string>(), correct.Count == 1 ? correct[0] : -1);
					break;

				case QuestionType.Multi:
					question = new MultiChoiceQuestion(text, points, options ?? Array.Empty<string>(), correct);
					break;

				case QuestionType.TrueFalse:
					if (!TryReadTrueFalse(correct, out var correctValue))
					{
						errors.Add(TrueFalseQuestion.OneAnswerMessage);
					}
					question = new TrueFalseQuestion(text, points, correctValue);
					break;

				default:
					throw new DomainValidationException($"Unknown question type '{type}'.");
			}

			errors.AddRange(question.Validate());
			if (errors.Count > 0)
			{
				throw new DomainValidationException(errors);
			}
			return question;
		}

		/// <summary>
		/// Updates an existing question. The new data is validated first on a throw-away question,
		/// so an invalid update never changes the real one. The type of a question can't change.
		/// </summary>
		public static void ApplyUpdate(
			Question question,
			string text,
			int points,
			IReadOnlyCollection<string>? options,
			IReadOnlyCollection<int>? correctOptionIndexes)
		{
			ArgumentNullException.ThrowIfNull(question);
			_ = Create(question.Type, text, points, options, correctOptionIndexes);

			var correct = Distinct(correctOptionIndexes);
			switch (question)
			{
				case SingleChoiceQuestion single:
					single.Update(text, points, options!, correct[0]);
					break;
				case MultiChoiceQuestion multi:
					multi.Update(text, points, options!, correct);
					break;
				case TrueFalseQuestion trueFalse:
					TryReadTrueFalse(correct, out var correctValue);
					trueFalse.Update(text, points, correctValue);
					break;
			}
		}

		private static List<int> Distinct(IReadOnlyCollection<int>? indexes)
		{
			return (indexes ?? Array.Empty<int>()).Distinct().ToList();
		}

		private static bool TryReadTrueFalse(List<int> correct, out bool value)
		{
			value = false;
			if (correct.Count != 1)
			{
				return false;
			}
			if (correct[0] == TrueFalseQuestion.TrueIndex)
			{
				value = true;
				return true;
			}
			return correct[0] == TrueFalseQuestion.FalseIndex;
		}
	}
}
