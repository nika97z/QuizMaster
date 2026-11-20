using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Factories;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Tests.TestHelpers;
using System.Reflection;

namespace QuizMaster.Tests.Domain
{
	public class QuestionTests
	{
		[Fact]
		public void SingleChoice_CheckAnswer_AcceptsOnlyTheCorrectOption()
		{
			var question = TestData.Single(1, correct: 1);

			Assert.True(question.CheckAnswer(new[] { 1 }));
			Assert.False(question.CheckAnswer(new[] { 0 }));
			Assert.False(question.CheckAnswer(new[] { 1, 2 }));
			Assert.False(question.CheckAnswer(Array.Empty<int>()));
		}

		[Fact]
		public void MultiChoice_CheckAnswer_NeedsExactlyTheCorrectSet()
		{
			var question = TestData.Multi(1, 3, 0, 2);

			Assert.True(question.CheckAnswer(new[] { 0, 2 }));
			Assert.True(question.CheckAnswer(new[] { 2, 0 }));
			Assert.False(question.CheckAnswer(new[] { 0 }));
			Assert.False(question.CheckAnswer(new[] { 0, 1, 2 }));
		}

		[Fact]
		public void MultiChoice_CheckAnswer_CountsRepeatedChoicesOnce()
		{
			var question = TestData.Multi(1, 3, 0, 2);

			Assert.True(question.CheckAnswer(new[] { 0, 0, 2, 2 }));
		}

		[Theory]
		[InlineData(true, TrueFalseQuestion.TrueIndex, true)]
		[InlineData(true, TrueFalseQuestion.FalseIndex, false)]
		[InlineData(false, TrueFalseQuestion.FalseIndex, true)]
		[InlineData(false, TrueFalseQuestion.TrueIndex, false)]
		public void TrueFalse_CheckAnswer(bool correctValue, int selected, bool expected)
		{
			var question = TestData.TrueFalse(1, correctValue);

			Assert.Equal(expected, question.CheckAnswer(new[] { selected }));
		}

		[Fact]
		public void Factory_SingleChoiceWithTwoCorrectOptions_IsRejected()
		{
			var ex = Assert.Throws<DomainValidationException>(() =>
				QuestionFactory.Create(QuestionType.Single, "Q", 1, new[] { "A", "B" }, new[] { 0, 1 }));

			Assert.Contains(SingleChoiceQuestion.ExactlyOneCorrectMessage, ex.Errors);
		}

		[Fact]
		public void Factory_SingleChoiceWithNoCorrectOption_IsRejected()
		{
			var ex = Assert.Throws<DomainValidationException>(() =>
				QuestionFactory.Create(QuestionType.Single, "Q", 1, new[] { "A", "B" }, Array.Empty<int>()));

			Assert.Contains(SingleChoiceQuestion.ExactlyOneCorrectMessage, ex.Errors);
		}

		[Fact]
		public void Factory_MultiChoiceWithNoCorrectOption_IsRejected()
		{
			var ex = Assert.Throws<DomainValidationException>(() =>
				QuestionFactory.Create(QuestionType.Multi, "Q", 2, new[] { "A", "B", "C" }, Array.Empty<int>()));

			Assert.Contains(MultiChoiceQuestion.AtLeastOneCorrectMessage, ex.Errors);
		}

		[Fact]
		public void Factory_TrueFalseWithAnswerOtherThanTrueOrFalse_IsRejected()
		{
			var ex = Assert.Throws<DomainValidationException>(() =>
				QuestionFactory.Create(QuestionType.TrueFalse, "Q", 1, null, new[] { 2 }));

			Assert.Contains(TrueFalseQuestion.OneAnswerMessage, ex.Errors);
		}

		[Fact]
		public void Factory_ReportsEveryProblemAtOnce()
		{
			var ex = Assert.Throws<DomainValidationException>(() =>
				QuestionFactory.Create(QuestionType.Single, " ", 0, new[] { "Only one" }, Array.Empty<int>()));

			Assert.Contains("Question text can't be empty.", ex.Errors);
			Assert.Contains("Points must be a positive number.", ex.Errors);
			Assert.Contains("The question needs at least 2 options.", ex.Errors);
			Assert.Contains(SingleChoiceQuestion.ExactlyOneCorrectMessage, ex.Errors);
		}

		[Fact]
		public void Factory_DuplicateOptions_AreRejected()
		{
			var ex = Assert.Throws<DomainValidationException>(() =>
				QuestionFactory.Create(QuestionType.Multi, "Q", 2, new[] { "Yes", "yes", "No" }, new[] { 0 }));

			Assert.Contains("Options must be different from each other.", ex.Errors);
		}

		[Fact]
		public void Factory_TrueFalse_GetsTheTwoFixedOptions()
		{
			var question = QuestionFactory.Create(QuestionType.TrueFalse, "Q", 1, null, new[] { TrueFalseQuestion.TrueIndex });

			Assert.IsType<TrueFalseQuestion>(question);
			Assert.Equal(new[] { "True", "False" }, question.Options.OrderBy(o => o.Position).Select(o => o.Text));
		}

		[Fact]
		public void Validate_CorrectIndexOutOfRange_IsReported()
		{
			var question = new SingleChoiceQuestion("Q", 1, new[] { "A", "B" }, 5);

			Assert.Contains(SingleChoiceQuestion.ExactlyOneCorrectMessage, question.Validate());
		}

		[Fact]
		public void DisplayQuestion_ShowsOptionsInTheGivenOrder()
		{
			var question = TestData.Single(1);

			var text = question.DisplayQuestion(new[] { 2, 0, 1 });

			Assert.Contains("1) C", text);
			Assert.Contains("2) A", text);
			Assert.Contains("3) B", text);
		}

		[Fact]
		public void DisplayQuestion_IsDifferentForEachType()
		{
			Assert.Contains("Choose ONE answer", TestData.Single(1).DisplayQuestion());
			Assert.Contains("Choose ALL correct answers", TestData.Multi(2).DisplayQuestion());
			Assert.Contains("True or false?", TestData.TrueFalse(3).DisplayQuestion());
		}

		[Fact]
		public void ValidateSelection_ChecksRangeAndCountPerType()
		{
			Assert.NotNull(TestData.Single(1).ValidateSelection(new[] { 5 }));
			Assert.NotNull(TestData.Single(1).ValidateSelection(new[] { 0, 1 }));
			Assert.NotNull(TestData.TrueFalse(1).ValidateSelection(Array.Empty<int>()));
			Assert.Null(TestData.Multi(1).ValidateSelection(new[] { 0, 1 }));
			Assert.Null(TestData.Multi(1).ValidateSelection(new[] { 0, 0 }));
		}

		[Theory]
		[InlineData(typeof(SingleChoiceQuestion), "_correctOptionIndex")]
		[InlineData(typeof(MultiChoiceQuestion), "_correctOptionIndexes")]
		[InlineData(typeof(TrueFalseQuestion), "_correctValue")]
		public void CorrectAnswer_IsAPrivateFieldWithNoPublicAccess(Type questionType, string fieldName)
		{
			var field = questionType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

			Assert.NotNull(field);
			Assert.True(field!.IsPrivate);
			Assert.DoesNotContain(questionType.GetProperties(), p => p.Name.Contains("Correct", StringComparison.OrdinalIgnoreCase));
		}
	}
}
