using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Helpers;
using QuizMaster.Domain.Models.Enums;
using System.Security.Cryptography;

namespace QuizMaster.Domain.Models.Entities
{
	/// <summary>
	/// One student's attempt at one quiz. Status and score are protected (private setters):
	/// they change only through Start(), SubmitAnswer(), Finish() and Cancel().
	/// </summary>
	public class Attempt
	{
		private const string CertificateAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

		public int Id { get; private set; }
		public int QuizId { get; private set; }
		public Quiz Quiz { get; private set; } = null!;
		public int StudentId { get; private set; }
		public Student Student { get; private set; } = null!;
		public DateTime StartedAt { get; private set; }
		public DateTime Deadline { get; private set; }
		public DateTime? FinishedAt { get; private set; }
		public AttemptStatus Status { get; private set; }
		public int Score { get; private set; }
		public int MaxScore { get; private set; }
		public decimal Percent { get; private set; }
		public AttemptResult Result { get; private set; }
		public string? CertificateCode { get; private set; }
		public string QuestionOrder { get; private set; } = string.Empty;
		public List<AnswerRecord> Answers { get; private set; } = new();

		private Attempt()
		{
		}

		public Attempt(Quiz quiz, Student student)
		{
			ArgumentNullException.ThrowIfNull(quiz);
			ArgumentNullException.ThrowIfNull(student);
			Quiz = quiz;
			QuizId = quiz.Id;
			Student = student;
			StudentId = student.Id;
		}

		public bool IsStarted => StartedAt != default;

		public int CorrectCount => Answers.Count(a => a.IsCorrect);

		public bool IsExpired(DateTime now)
		{
			return Status == AttemptStatus.InProgress && IsStarted && now > Deadline;
		}

		/// <summary>
		/// Starts the attempt: records the start time and deadline, sets InProgress and keeps the question order.
		/// </summary>
		public void Start(DateTime now, IEnumerable<int> questionOrder)
		{
			if (IsStarted)
			{
				throw new InvalidAttemptStateException("This attempt has already been started.");
			}
			if (!Quiz.IsPublished)
			{
				throw new InvalidAttemptStateException("Only a published quiz can be started.");
			}
			if (Quiz.Questions.Count == 0)
			{
				throw new InvalidAttemptStateException("This quiz has no questions.");
			}

			var order = (questionOrder ?? Enumerable.Empty<int>()).ToList();
			var quizQuestionIds = Quiz.Questions.Select(q => q.Id).OrderBy(id => id);
			if (!order.OrderBy(id => id).SequenceEqual(quizQuestionIds))
			{
				throw new DomainValidationException("The question order must contain every question of the quiz exactly once.");
			}

			StartedAt = now;
			Deadline = now.AddMinutes(Quiz.TimeLimitMinutes);
			Status = AttemptStatus.InProgress;
			Result = AttemptResult.None;
			MaxScore = Quiz.MaxScore;
			QuestionOrder = IndexList.Join(order);
		}

		/// <summary>
		/// Saves (or replaces) the answer to one question. Selected indexes are original option positions.
		/// </summary>
		public AnswerRecord SubmitAnswer(Question question, IReadOnlyCollection<int> selectedOptionIndexes, DateTime? now = null)
		{
			ArgumentNullException.ThrowIfNull(question);
			var answeredAt = now ?? DateTime.Now;

			EnsureInProgress();
			if (answeredAt > Deadline)
			{
				throw new TimeLimitExceededException();
			}
			if (question.QuizId != QuizId)
			{
				throw new DomainValidationException("This question doesn't belong to the attempt's quiz.");
			}

			var error = question.ValidateSelection(selectedOptionIndexes);
			if (error != null)
			{
				throw new DomainValidationException(error);
			}

			var normalized = selectedOptionIndexes.Distinct().OrderBy(i => i).ToList();
			var isCorrect = question.CheckAnswer(normalized);

			var record = Answers.FirstOrDefault(a => a.QuestionId == question.Id);
			if (record == null)
			{
				record = new AnswerRecord(question.Id);
				Answers.Add(record);
			}
			record.Record(normalized, isCorrect, isCorrect ? question.Points : 0, answeredAt);
			return record;
		}

		/// <summary>
		/// Finishes the attempt: calculates score, percent and Passed/Failed. Unanswered questions count as incorrect.
		/// </summary>
		public void Finish(int passPercent, DateTime? now = null)
		{
			EnsureInProgress();
			if (passPercent < Quiz.MinPassPercent || passPercent > Quiz.MaxPassPercent)
			{
				throw new DomainValidationException($"Pass percent must be between {Quiz.MinPassPercent} and {Quiz.MaxPassPercent}.");
			}

			var score = Answers.Sum(a => a.PointsAwarded);
			if (score < 0 || score > MaxScore)
			{
				throw new DomainValidationException($"The score {score} is outside the allowed range 0-{MaxScore}.");
			}

			var finishedAt = now ?? DateTime.Now;
			Score = score;
			Percent = CalculatePercent(score, MaxScore);
			Result = Percent >= passPercent ? AttemptResult.Passed : AttemptResult.Failed;
			FinishedAt = finishedAt;
			Status = AttemptStatus.Finished;
			CertificateCode = Result == AttemptResult.Passed ? CreateCertificateCode(finishedAt) : null;
		}

		public void Cancel(DateTime? now = null)
		{
			EnsureInProgress();
			Status = AttemptStatus.Cancelled;
			FinishedAt = now ?? DateTime.Now;
			Result = AttemptResult.None;
			Score = 0;
			Percent = 0;
			CertificateCode = null;
		}

		public IReadOnlyList<int> GetQuestionOrder()
		{
			return IndexList.TryParse(QuestionOrder, out var ids) ? ids : new List<int>();
		}

		public static decimal CalculatePercent(int score, int maxScore)
		{
			if (maxScore <= 0)
			{
				return 0m;
			}
			return Math.Round(score * 100m / maxScore, 2, MidpointRounding.AwayFromZero);
		}

		private void EnsureInProgress()
		{
			if (!IsStarted)
			{
				throw new InvalidAttemptStateException("This attempt hasn't been started.");
			}
			if (Status != AttemptStatus.InProgress)
			{
				throw new InvalidAttemptStateException($"This attempt is already {Status.ToString().ToLowerInvariant()}. No more changes are allowed.");
			}
		}

		private string CreateCertificateCode(DateTime date)
		{
			return $"QM-{date:yyyyMMdd}-{Id:D5}-{RandomNumberGenerator.GetString(CertificateAlphabet, 6)}";
		}
	}
}
