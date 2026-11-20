using Moq;
using QuizMaster.Application.Exceptions;
using QuizMaster.Application.Services.Implementations;
using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Tests.TestHelpers;

namespace QuizMaster.Tests.Services
{
	public class QuizServiceTests
	{
		private readonly Mock<IQuizRepository> _quizRepository = new();
		private readonly Mock<IUserRepository> _userRepository = new();
		private readonly Mock<IUnitOfWork> _unitOfWork = new();
		private readonly Mock<IAppLogger> _logger = new();
		private readonly QuizService _quizService;
		private readonly Lecturer _lecturer = TestData.Lecturer(1);

		public QuizServiceTests()
		{
			_userRepository.Setup(r => r.GetUserById(1)).Returns(_lecturer);
			_userRepository.Setup(r => r.GetUserById(2)).Returns(TestData.Student(2));
			_userRepository.Setup(r => r.GetUserById(5)).Returns(TestData.Lecturer(5, "other"));
			_quizService = new QuizService(_quizRepository.Object, _userRepository.Object, TestMapper.Create(),
				_unitOfWork.Object, _logger.Object);
		}

		private static CreateQuizDTO ValidQuiz()
		{
			return new CreateQuizDTO { Title = "New quiz", Topic = "Programming", TimeLimitMinutes = 15, PassPercent = 60 };
		}

		[Fact]
		public void CreateQuiz_ByAStudent_IsUnauthorized()
		{
			Assert.Throws<UnauthorizedActionException>(() => _quizService.CreateQuiz(2, ValidQuiz()));

			_quizRepository.Verify(r => r.AddQuiz(It.IsAny<Quiz>()), Times.Never);
		}

		[Fact]
		public void CreateQuiz_InvalidSettings_IsRejected()
		{
			var dto = ValidQuiz();
			dto.TimeLimitMinutes = 0;

			Assert.Throws<DomainValidationException>(() => _quizService.CreateQuiz(1, dto));
			_quizRepository.Verify(r => r.AddQuiz(It.IsAny<Quiz>()), Times.Never);
		}

		[Fact]
		public void CreateQuiz_Valid_SavesADraftAndLogs()
		{
			var result = _quizService.CreateQuiz(1, ValidQuiz());

			_quizRepository.Verify(r => r.AddQuiz(It.Is<Quiz>(q =>
				q.Title == "New quiz" && q.LecturerId == 1 && !q.IsPublished)), Times.Once);
			_unitOfWork.Verify(u => u.SaveChanges(), Times.Once);
			_logger.Verify(l => l.Log(LogEventType.QuizCreated, It.IsAny<string>(), 1, It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
			Assert.Equal("lela", result.LecturerName);
		}

		[Fact]
		public void UpdateQuiz_AnotherLecturersQuiz_IsUnauthorized()
		{
			var quiz = TestData.PublishedQuiz(10, _lecturer);
			quiz.Close();
			_quizRepository.Setup(r => r.GetQuizWithQuestions(10)).Returns(quiz);

			var dto = new UpdateQuizDTO { Id = 10, Title = "Hijacked", Topic = "X", TimeLimitMinutes = 10, PassPercent = 50 };

			Assert.Throws<UnauthorizedActionException>(() => _quizService.UpdateQuiz(5, dto));
			Assert.Equal("Quiz 10", quiz.Title);
		}

		[Fact]
		public void UpdateQuiz_PublishedQuiz_IsRejected()
		{
			var quiz = TestData.PublishedQuiz(10, _lecturer);
			_quizRepository.Setup(r => r.GetQuizWithQuestions(10)).Returns(quiz);

			var dto = new UpdateQuizDTO { Id = 10, Title = "Changed", Topic = "X", TimeLimitMinutes = 10, PassPercent = 50 };

			Assert.Throws<DomainValidationException>(() => _quizService.UpdateQuiz(1, dto));
			_unitOfWork.Verify(u => u.SaveChanges(), Times.Never);
		}

		[Fact]
		public void PublishQuiz_EmptyQuiz_IsRejected()
		{
			var quiz = new Quiz { Id = 11, Title = "Empty", Topic = "X", TimeLimitMinutes = 10, PassPercent = 50, LecturerId = 1, Lecturer = _lecturer };
			_quizRepository.Setup(r => r.GetQuizWithQuestions(11)).Returns(quiz);

			Assert.Throws<DomainValidationException>(() => _quizService.PublishQuiz(1, 11));

			Assert.False(quiz.IsPublished);
			_unitOfWork.Verify(u => u.SaveChanges(), Times.Never);
		}

		[Fact]
		public void DeleteQuiz_WithAttempts_IsRejected()
		{
			var quiz = TestData.PublishedQuiz(10, _lecturer);
			_quizRepository.Setup(r => r.GetQuizWithQuestions(10)).Returns(quiz);
			_quizRepository.Setup(r => r.HasAttempts(10)).Returns(true);

			Assert.Throws<DomainValidationException>(() => _quizService.DeleteQuiz(1, 10));
			_quizRepository.Verify(r => r.DeleteQuiz(It.IsAny<Quiz>()), Times.Never);
		}

		[Fact]
		public void GetPublishedQuizzes_MapsCatalogFields()
		{
			_quizRepository.Setup(r => r.GetPublishedQuizzes()).Returns(new List<Quiz> { TestData.PublishedQuiz(10, _lecturer) });

			var quiz = Assert.Single(_quizService.GetPublishedQuizzes());

			Assert.Equal(3, quiz.QuestionCount);
			Assert.Equal(6, quiz.MaxScore);
			Assert.Equal(20, quiz.TimeLimitMinutes);
			Assert.True(quiz.IsPublished);
		}
	}
}
