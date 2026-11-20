using Moq;
using QuizMaster.Application.Exceptions;
using QuizMaster.Application.Services.Implementations;
using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuestionDTOs;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Tests.TestHelpers;

namespace QuizMaster.Tests.Services
{
	public class QuizGenerationServiceTests
	{
		private readonly Mock<IQuizGenerator> _generator = new();
		private readonly Mock<IQuizRepository> _quizRepository = new();
		private readonly Mock<IUserRepository> _userRepository = new();
		private readonly Mock<IUnitOfWork> _unitOfWork = new();
		private readonly Mock<IAppLogger> _logger = new();
		private readonly QuizGenerationService _service;
		private Quiz? _saved;

		public QuizGenerationServiceTests()
		{
			_userRepository.Setup(r => r.GetUserById(1)).Returns(TestData.Lecturer(1));
			_userRepository.Setup(r => r.GetUserById(2)).Returns(TestData.Student(2));
			_generator.Setup(g => g.IsConfigured).Returns(true);
			_quizRepository.Setup(r => r.AddQuiz(It.IsAny<Quiz>())).Callback<Quiz>(q => { q.Id = 42; _saved = q; });
			_service = new QuizGenerationService(_generator.Object, _quizRepository.Object, _userRepository.Object,
				_unitOfWork.Object, _logger.Object);
		}

		private static GenerateQuizDTO Request(string subject = "Photosynthesis", int questionCount = 3)
		{
			return new GenerateQuizDTO { Subject = subject, QuestionCount = questionCount, TimeLimitMinutes = 10, PassPercent = 60 };
		}

		private static CreateQuestionDTO Single(string text = "Which pigment captures light?", params int[] correct)
		{
			return new CreateQuestionDTO
			{
				Type = QuestionType.Single,
				Text = text,
				Points = 2,
				Options = new List<string> { "Chlorophyll", "Melanin", "Keratin" },
				CorrectOptionIndexes = correct.Length == 0 ? new List<int> { 0 } : correct.ToList()
			};
		}

		private static CreateQuestionDTO Multi()
		{
			return new CreateQuestionDTO
			{
				Type = QuestionType.Multi,
				Text = "Which are products of photosynthesis?",
				Points = 3,
				Options = new List<string> { "Oxygen", "Glucose", "Nitrogen", "Iron" },
				CorrectOptionIndexes = new List<int> { 0, 1 }
			};
		}

		private static CreateQuestionDTO TrueFalse()
		{
			return new CreateQuestionDTO
			{
				Type = QuestionType.TrueFalse,
				Text = "Photosynthesis takes place in chloroplasts.",
				Points = 1,
				CorrectOptionIndexes = new List<int> { TrueFalseQuestion.TrueIndex }
			};
		}

		private void AiWrites(GeneratedQuizDTO quiz)
		{
			_generator.Setup(g => g.Generate(It.IsAny<string>(), It.IsAny<int>())).Returns(quiz);
		}

		[Fact]
		public void GenerateQuiz_WithASubject_SavesADraftWithEveryQuestion()
		{
			AiWrites(new GeneratedQuizDTO { Title = "Photosynthesis Basics", Topic = "Biology", Questions = { Single(), Multi(), TrueFalse() } });

			var result = _service.GenerateQuiz(1, Request());

			_generator.Verify(g => g.Generate("Photosynthesis", 3), Times.Once);
			Assert.NotNull(_saved);
			Assert.False(_saved!.IsPublished);
			Assert.Equal("Photosynthesis Basics", _saved.Title);
			Assert.Equal("Biology", _saved.Topic);
			Assert.Equal(10, _saved.TimeLimitMinutes);
			Assert.Equal(60, _saved.PassPercent);
			Assert.Equal(1, _saved.LecturerId);
			Assert.Equal(new[] { QuestionType.Single, QuestionType.Multi, QuestionType.TrueFalse }, _saved.Questions.Select(q => q.Type));
			Assert.True(_saved.Questions[2].CheckAnswer(new[] { TrueFalseQuestion.TrueIndex }));
			Assert.Equal(42, result.QuizId);
			Assert.Equal(3, result.QuestionCount);
			Assert.Equal(0, result.SkippedQuestions);
			Assert.Equal("Photosynthesis", result.Subject);
			_unitOfWork.Verify(u => u.SaveChanges(), Times.Once);
			_logger.Verify(l => l.Log(LogEventType.QuizGenerated, It.IsAny<string>(), 1, 42, It.IsAny<int?>()), Times.Once);
		}

		[Fact]
		public void GenerateQuiz_RandomSubject_AsksForOneOfTheListedSubjects()
		{
			string? asked = null;
			_generator.Setup(g => g.Generate(It.IsAny<string>(), 1))
				.Callback<string, int>((subject, _) => asked = subject)
				.Returns(new GeneratedQuizDTO { Title = "Quiz", Topic = "Any", Questions = { TrueFalse() } });

			var result = _service.GenerateQuiz(1, new GenerateQuizDTO
			{
				RandomSubject = true,
				Subject = "ignored",
				QuestionCount = 1,
				TimeLimitMinutes = 5,
				PassPercent = 50
			});

			Assert.Contains(asked, QuizGenerationService.RandomSubjects);
			Assert.Equal(asked, result.Subject);
		}

		[Fact]
		public void GenerateQuiz_QuestionsThatFailTheChecks_AreLeftOut()
		{
			var twoCorrectOnASingle = Single("Which two?", 0, 1);
			var tooLong = Single(new string('x', Question.MaxTextLength + 1));
			AiWrites(new GeneratedQuizDTO { Title = "Quiz", Topic = "Biology", Questions = { Single(), twoCorrectOnASingle, tooLong } });

			var result = _service.GenerateQuiz(1, Request());

			Assert.Single(_saved!.Questions);
			Assert.Equal(1, result.QuestionCount);
			Assert.Equal(2, result.SkippedQuestions);
		}

		[Fact]
		public void GenerateQuiz_NoUsableQuestion_SavesNothing()
		{
			AiWrites(new GeneratedQuizDTO { Title = "Quiz", Topic = "Biology", Questions = { Single("Which two?", 0, 1) } });

			Assert.Throws<QuizGenerationException>(() => _service.GenerateQuiz(1, Request()));

			_quizRepository.Verify(r => r.AddQuiz(It.IsAny<Quiz>()), Times.Never);
			_unitOfWork.Verify(u => u.SaveChanges(), Times.Never);
		}

		[Fact]
		public void GenerateQuiz_InvalidRequest_ReportsEveryProblemWithoutAskingTheAi()
		{
			var request = new GenerateQuizDTO { Subject = "  ", QuestionCount = 0, TimeLimitMinutes = 0, PassPercent = 101 };

			var ex = Assert.Throws<DomainValidationException>(() => _service.GenerateQuiz(1, request));

			Assert.Equal(4, ex.Errors.Count);
			_generator.Verify(g => g.Generate(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
		}

		[Fact]
		public void GenerateQuiz_ByAStudent_IsUnauthorized()
		{
			Assert.Throws<UnauthorizedActionException>(() => _service.GenerateQuiz(2, Request()));
			_generator.Verify(g => g.Generate(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
		}

		[Fact]
		public void GenerateQuiz_WithoutAnApiKey_Fails()
		{
			_generator.Setup(g => g.IsConfigured).Returns(false);

			Assert.False(_service.IsAvailable);
			Assert.Throws<QuizGenerationException>(() => _service.GenerateQuiz(1, Request()));
			_generator.Verify(g => g.Generate(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
		}

		[Fact]
		public void GenerateQuiz_LongTitleAndMissingTopic_FitTheDatabase()
		{
			AiWrites(new GeneratedQuizDTO { Title = new string('T', 300), Topic = "", Questions = { TrueFalse() } });

			_service.GenerateQuiz(1, Request("Photosynthesis", 1));

			Assert.Equal(Quiz.MaxTitleLength, _saved!.Title.Length);
			Assert.EndsWith("...", _saved.Title);
			Assert.Equal("Photosynthesis", _saved.Topic);
		}
	}
}
