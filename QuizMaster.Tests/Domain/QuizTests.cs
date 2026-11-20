using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Tests.TestHelpers;

namespace QuizMaster.Tests.Domain
{
	public class QuizTests
	{
		private static Quiz DraftQuiz(int timeLimit = 20, int passPercent = 70)
		{
			return new Quiz { Id = 1, Title = "Draft", Topic = "Programming", TimeLimitMinutes = timeLimit, PassPercent = passPercent };
		}

		[Fact]
		public void Publish_EmptyQuiz_IsRejected()
		{
			var quiz = DraftQuiz();

			var ex = Assert.Throws<DomainValidationException>(quiz.Publish);

			Assert.Contains(ex.Errors, e => e.Contains("empty quiz"));
			Assert.False(quiz.IsPublished);
		}

		[Theory]
		[InlineData(0, 70)]
		[InlineData(301, 70)]
		[InlineData(20, 0)]
		[InlineData(20, 101)]
		public void ValidateSettings_OutOfRangeValues_AreReported(int timeLimit, int passPercent)
		{
			var quiz = DraftQuiz(timeLimit, passPercent);

			Assert.NotEmpty(quiz.ValidateSettings());
		}

		[Fact]
		public void Publish_InvalidQuestion_IsRejectedWithTheQuestionNumber()
		{
			var quiz = DraftQuiz();
			quiz.AddQuestion(TestData.Single(1));
			quiz.AddQuestion(new SingleChoiceQuestion("Broken", 0, new[] { "A", "B" }, 0));

			var ex = Assert.Throws<DomainValidationException>(quiz.Publish);

			Assert.Contains(ex.Errors, e => e.StartsWith("Question 2:"));
		}

		[Fact]
		public void Publish_ValidQuiz_MakesItPublished()
		{
			var quiz = DraftQuiz();
			quiz.AddQuestion(TestData.Single(1));

			quiz.Publish();

			Assert.True(quiz.IsPublished);
		}

		[Fact]
		public void EnsureEditable_PublishedQuiz_Throws_UntilItIsClosed()
		{
			var quiz = TestData.PublishedQuiz();

			Assert.Throws<DomainValidationException>(quiz.EnsureEditable);
			quiz.Close();
			quiz.EnsureEditable();
			Assert.False(quiz.IsPublished);
		}

		[Fact]
		public void MaxScore_IsTheSumOfAllPoints()
		{
			var quiz = TestData.PublishedQuiz();

			Assert.Equal(6, quiz.MaxScore);
			Assert.Equal(3, quiz.QuestionCount);
		}

		[Fact]
		public void AddQuestion_SetsQuizIdAndOrder()
		{
			var quiz = DraftQuiz();
			var first = TestData.Single(1);
			var second = TestData.TrueFalse(2);

			quiz.AddQuestion(first);
			quiz.AddQuestion(second);

			Assert.Equal(quiz.Id, second.QuizId);
			Assert.Equal(1, first.OrderIndex);
			Assert.Equal(2, second.OrderIndex);
		}
	}
}
