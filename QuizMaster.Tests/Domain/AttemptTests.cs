using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Tests.TestHelpers;

namespace QuizMaster.Tests.Domain
{
	public class AttemptTests
	{
		private readonly Quiz _quiz = TestData.PublishedQuiz(passPercent: 50);
		private readonly Student _student = TestData.Student();

		private Question Single => _quiz.Questions.OfType<SingleChoiceQuestion>().First();
		private Question Multi => _quiz.Questions.OfType<MultiChoiceQuestion>().First();
		private Question TrueFalse => _quiz.Questions.OfType<TrueFalseQuestion>().First();

		[Fact]
		public void Start_SetsInProgressStartTimeDeadlineAndMaxScore()
		{
			var attempt = TestData.StartedAttempt(_quiz, _student);

			Assert.Equal(AttemptStatus.InProgress, attempt.Status);
			Assert.Equal(TestData.Now, attempt.StartedAt);
			Assert.Equal(TestData.Now.AddMinutes(20), attempt.Deadline);
			Assert.Equal(6, attempt.MaxScore);
		}

		[Fact]
		public void Start_UnpublishedQuiz_Throws()
		{
			_quiz.Close();
			var attempt = new Attempt(_quiz, _student);

			Assert.Throws<InvalidAttemptStateException>(() => attempt.Start(TestData.Now, _quiz.Questions.Select(q => q.Id)));
		}

		[Fact]
		public void Start_Twice_Throws()
		{
			var attempt = TestData.StartedAttempt(_quiz, _student);

			Assert.Throws<InvalidAttemptStateException>(() => attempt.Start(TestData.Now, _quiz.Questions.Select(q => q.Id)));
		}

		[Fact]
		public void SubmitAnswer_AfterFinish_Throws()
		{
			var attempt = TestData.StartedAttempt(_quiz, _student);
			attempt.Finish(_quiz.PassPercent, TestData.Now.AddMinutes(2));

			Assert.Throws<InvalidAttemptStateException>(() => attempt.SubmitAnswer(Single, new[] { 1 }, TestData.Now.AddMinutes(3)));
		}

		[Fact]
		public void SubmitAnswer_AfterCancel_Throws()
		{
			var attempt = TestData.StartedAttempt(_quiz, _student);
			attempt.Cancel(TestData.Now.AddMinutes(1));

			Assert.Throws<InvalidAttemptStateException>(() => attempt.SubmitAnswer(Single, new[] { 1 }, TestData.Now.AddMinutes(2)));
		}

		[Fact]
		public void SubmitAnswer_AfterTheDeadline_ThrowsTimeLimitExceeded()
		{
			var attempt = TestData.StartedAttempt(_quiz, _student);

			Assert.Throws<TimeLimitExceededException>(() => attempt.SubmitAnswer(Single, new[] { 1 }, TestData.Now.AddMinutes(21)));
		}

		[Fact]
		public void SubmitAnswer_IndexThatDoesNotExist_IsRejected()
		{
			var attempt = TestData.StartedAttempt(_quiz, _student);

			Assert.Throws<DomainValidationException>(() => attempt.SubmitAnswer(Single, new[] { 7 }, TestData.Now.AddMinutes(1)));
			Assert.Empty(attempt.Answers);
		}

		[Fact]
		public void SubmitAnswer_SameQuestionTwice_ReplacesTheAnswer()
		{
			var attempt = TestData.StartedAttempt(_quiz, _student);

			attempt.SubmitAnswer(Single, new[] { 0 }, TestData.Now.AddMinutes(1));
			attempt.SubmitAnswer(Single, new[] { 1 }, TestData.Now.AddMinutes(2));

			var record = Assert.Single(attempt.Answers);
			Assert.True(record.IsCorrect);
			Assert.Equal(2, record.PointsAwarded);
		}

		[Fact]
		public void SubmitAnswer_MultiChoiceRepeatedChoices_CountOnce()
		{
			var attempt = TestData.StartedAttempt(_quiz, _student);

			var record = attempt.SubmitAnswer(Multi, new[] { 0, 0, 2 }, TestData.Now.AddMinutes(1));

			Assert.True(record.IsCorrect);
			Assert.Equal(new[] { 0, 2 }, record.GetSelectedIndexes());
		}

		[Fact]
		public void Finish_CalculatesScorePercentResultAndCertificate()
		{
			var attempt = TestData.FinishedAttempt(_quiz, _student, correctAnswers: 2, TestData.Now);

			Assert.Equal(AttemptStatus.Finished, attempt.Status);
			Assert.Equal(5, attempt.Score);
			Assert.Equal(83.33m, attempt.Percent);
			Assert.Equal(AttemptResult.Passed, attempt.Result);
			Assert.Equal(2, attempt.CorrectCount);
			Assert.Matches(@"^QM-\d{8}-\d{5}-[A-Z0-9]{6}$", attempt.CertificateCode);
		}

		[Fact]
		public void Finish_BelowThePassMark_FailsWithoutCertificate()
		{
			var attempt = TestData.FinishedAttempt(_quiz, _student, correctAnswers: 1, TestData.Now);

			Assert.Equal(2, attempt.Score);
			Assert.Equal(33.33m, attempt.Percent);
			Assert.Equal(AttemptResult.Failed, attempt.Result);
			Assert.Null(attempt.CertificateCode);
		}

		[Fact]
		public void Finish_ExactlyThePassMark_Passes()
		{
			var attempt = TestData.StartedAttempt(_quiz, _student);
			attempt.SubmitAnswer(Single, new[] { 1 }, TestData.Now.AddMinutes(1));
			attempt.SubmitAnswer(TrueFalse, new[] { TrueFalseQuestion.FalseIndex }, TestData.Now.AddMinutes(2));

			attempt.Finish(50, TestData.Now.AddMinutes(3));

			Assert.Equal(50m, attempt.Percent);
			Assert.Equal(AttemptResult.Passed, attempt.Result);
		}

		[Fact]
		public void Finish_UnansweredQuestions_CountAsIncorrect()
		{
			var attempt = TestData.StartedAttempt(_quiz, _student);
			attempt.SubmitAnswer(TrueFalse, new[] { TrueFalseQuestion.FalseIndex }, TestData.Now.AddMinutes(1));

			attempt.Finish(_quiz.PassPercent, TestData.Now.AddMinutes(2));

			Assert.Equal(1, attempt.Score);
			Assert.Equal(16.67m, attempt.Percent);
		}

		[Theory]
		[InlineData(2, 3, "66.67")]
		[InlineData(1, 3, "33.33")]
		[InlineData(1, 8, "12.5")]
		[InlineData(20, 20, "100")]
		[InlineData(0, 20, "0")]
		public void CalculatePercent_RoundsToTwoDecimals(int score, int maxScore, string expected)
		{
			Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), Attempt.CalculatePercent(score, maxScore));
		}

		[Fact]
		public void Cancel_SetsCancelledWithNoResult()
		{
			var attempt = TestData.StartedAttempt(_quiz, _student);

			attempt.Cancel(TestData.Now.AddMinutes(1));

			Assert.Equal(AttemptStatus.Cancelled, attempt.Status);
			Assert.Equal(AttemptResult.None, attempt.Result);
		}

		[Fact]
		public void Cancel_AfterFinish_Throws()
		{
			var attempt = TestData.FinishedAttempt(_quiz, _student, correctAnswers: 3, TestData.Now);

			Assert.Throws<InvalidAttemptStateException>(() => attempt.Cancel());
		}

		[Fact]
		public void IsExpired_OnlyAfterTheDeadline()
		{
			var attempt = TestData.StartedAttempt(_quiz, _student);

			Assert.False(attempt.IsExpired(TestData.Now.AddMinutes(19)));
			Assert.True(attempt.IsExpired(TestData.Now.AddMinutes(21)));
		}
	}
}
