using QuizMaster.Domain.Helpers;

namespace QuizMaster.Domain.Models.Entities
{
	/// <summary>
	/// A student's answer to one question within an attempt. Created and changed only by Attempt.SubmitAnswer().
	/// </summary>
	public class AnswerRecord
	{
		public int Id { get; private set; }
		public int AttemptId { get; private set; }
		public int QuestionId { get; private set; }
		public string SelectedOptionIndexes { get; private set; } = string.Empty;
		public bool IsCorrect { get; private set; }
		public int PointsAwarded { get; private set; }
		public DateTime AnsweredAt { get; private set; }

		private AnswerRecord()
		{
		}

		internal AnswerRecord(int questionId)
		{
			QuestionId = questionId;
		}

		internal void Record(IReadOnlyCollection<int> selectedOptionIndexes, bool isCorrect, int pointsAwarded, DateTime answeredAt)
		{
			SelectedOptionIndexes = IndexList.JoinDistinctSorted(selectedOptionIndexes);
			IsCorrect = isCorrect;
			PointsAwarded = pointsAwarded;
			AnsweredAt = answeredAt;
		}

		public IReadOnlyList<int> GetSelectedIndexes()
		{
			return IndexList.TryParse(SelectedOptionIndexes, out var indexes) ? indexes : new List<int>();
		}
	}
}
