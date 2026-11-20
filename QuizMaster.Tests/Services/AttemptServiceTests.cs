using Moq;
using QuizMaster.Application.Exceptions;
using QuizMaster.Application.Services.Implementations;
using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.AttemptDTOs;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Tests.TestHelpers;

namespace QuizMaster.Tests.Services
{
	public class AttemptServiceTests
	{
		private readonly Mock<IAttemptRepository> _attemptRepository = new();
		private readonly Mock<IQuizRepository> _quizRepository = new();
		private readonly Mock<IUserRepository> _userRepository = new();
		private readonly Mock<IUnitOfWork> _unitOfWork = new();
		private readonly Mock<IAppLogger> _logger = new();
		private readonly Mock<IEmailService> _emailService = new();
		private readonly AttemptService _attemptService;
		private readonly Student _student = TestData.Student(2);
		private readonly Quiz _quiz = TestData.PublishedQuiz(10, passPercent: 50);

		public AttemptServiceTests()
		{
			_userRepository.Setup(r => r.GetUserById(1)).Returns(TestData.Lecturer(1));
			_userRepository.Setup(r => r.GetUserById(2)).Returns(_student);
			_userRepository.Setup(r => r.GetUserById(3)).Returns(TestData.Student(3, "nino"));
			_quizRepository.Setup(r => r.GetQuizWithQuestions(10)).Returns(_quiz);
			_attemptService = new AttemptService(_attemptRepository.Object, _quizRepository.Object, _userRepository.Object,
				TestMapper.Create(), _unitOfWork.Object, _logger.Object, _emailService.Object);
		}

		private Attempt RunningAttempt(int id = 77)
		{
			var attempt = TestData.StartedAttempt(_quiz, _student, DateTime.Now, id);
			_attemptRepository.Setup(r => r.GetAttemptById(id)).Returns(attempt);
			return attempt;
		}

		private void VerifyLogged(LogEventType eventType, Times times)
		{
			_logger.Verify(l => l.Log(eventType, It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()), times);
		}

		[Fact]
		public void StartAttempt_CreatesAnInProgressAttemptWithEveryQuestion()
		{
			Attempt? created = null;
			_attemptRepository.Setup(r => r.AddAttempt(It.IsAny<Attempt>()))
				.Callback<Attempt>(a => { TestData.SetId(a, 77); created = a; });

			var session = _attemptService.StartAttempt(2, 10);

			Assert.NotNull(created);
			Assert.Equal(AttemptStatus.InProgress, created!.Status);
			Assert.Equal(77, session.AttemptId);
			Assert.Equal(3, session.TotalQuestions);
			Assert.Equal(_quiz.Questions.Select(q => q.Id).OrderBy(id => id), session.Questions.Select(q => q.Id).OrderBy(id => id));
			_unitOfWork.Verify(u => u.SaveChanges(), Times.Once);
			_logger.Verify(l => l.Log(LogEventType.QuizStarted, It.IsAny<string>(), 2, 10, 77), Times.Once);
		}

		[Fact]
		public void StartAttempt_WithAnActiveAttempt_ContinuesItInsteadOfCreatingAnother()
		{
			var active = TestData.StartedAttempt(_quiz, _student, DateTime.Now, 50);
			var first = _quiz.Questions.First();
			active.SubmitAnswer(first, TestData.CorrectAnswer(first), DateTime.Now);
			_attemptRepository.Setup(r => r.GetActiveAttempt(2, 10)).Returns(active);

			var session = _attemptService.StartAttempt(2, 10);

			Assert.True(session.Resumed);
			Assert.Equal(50, session.AttemptId);
			Assert.Equal(1, session.AnsweredCount);
			Assert.Equal(2, session.Questions.Count);
			_attemptRepository.Verify(r => r.AddAttempt(It.IsAny<Attempt>()), Times.Never);
		}

		[Fact]
		public void StartAttempt_WithAnExpiredActiveAttempt_CancelsItAndStartsANewOne()
		{
			var expired = TestData.StartedAttempt(_quiz, _student, DateTime.Now.AddHours(-1), 50);
			_attemptRepository.Setup(r => r.GetActiveAttempt(2, 10)).Returns(expired);

			var session = _attemptService.StartAttempt(2, 10);

			Assert.Equal(AttemptStatus.Cancelled, expired.Status);
			Assert.False(session.Resumed);
			_attemptRepository.Verify(r => r.AddAttempt(It.IsAny<Attempt>()), Times.Once);
			_logger.Verify(l => l.Log(LogEventType.AttemptTimedOut, It.IsAny<string>(), 2, 10, 50), Times.Once);
		}

		[Fact]
		public void StartAttempt_UnpublishedQuiz_IsRejected()
		{
			_quiz.Close();

			Assert.Throws<DomainValidationException>(() => _attemptService.StartAttempt(2, 10));
		}

		[Fact]
		public void StartAttempt_ByALecturer_IsUnauthorized()
		{
			Assert.Throws<UnauthorizedActionException>(() => _attemptService.StartAttempt(1, 10));
		}

		[Fact]
		public void GetAttemptSession_OwnInProgressAttempt_ReturnsOnlyTheUnansweredQuestions()
		{
			var attempt = RunningAttempt(77);
			var answered = _quiz.Questions.First();
			attempt.SubmitAnswer(answered, TestData.CorrectAnswer(answered), DateTime.Now);

			var session = _attemptService.GetAttemptSession(2, 77);

			Assert.Equal(77, session.AttemptId);
			Assert.False(session.Resumed);
			Assert.Equal(attempt.Deadline, session.Deadline);
			Assert.Equal(3, session.TotalQuestions);
			Assert.Equal(1, session.AnsweredCount);
			Assert.Equal(2, session.Questions.Count);
			Assert.DoesNotContain(session.Questions, q => q.Id == answered.Id);

			// Read-only: nothing is saved, changed or logged.
			_unitOfWork.Verify(u => u.SaveChanges(), Times.Never);
			_attemptRepository.Verify(r => r.UpdateAttempt(It.IsAny<Attempt>()), Times.Never);
			_logger.Verify(l => l.Log(It.IsAny<LogEventType>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
		}

		[Fact]
		public void GetAttemptSession_AnotherStudentsAttempt_IsUnauthorized()
		{
			RunningAttempt(77);

			Assert.Throws<UnauthorizedActionException>(() => _attemptService.GetAttemptSession(3, 77));
		}

		[Theory]
		[InlineData(AttemptStatus.Finished)]
		[InlineData(AttemptStatus.Cancelled)]
		public void GetAttemptSession_AttemptThatIsNotInProgress_IsRejected(AttemptStatus status)
		{
			var attempt = RunningAttempt(77);
			if (status == AttemptStatus.Finished)
			{
				attempt.Finish(_quiz.PassPercent, DateTime.Now);
			}
			else
			{
				attempt.Cancel(DateTime.Now);
			}

			Assert.Throws<InvalidAttemptStateException>(() => _attemptService.GetAttemptSession(2, 77));
		}

		[Fact]
		public void SubmitAnswer_ToAnotherStudentsAttempt_IsUnauthorized()
		{
			RunningAttempt(77);
			var answer = new SubmitAnswerDTO { AttemptId = 77, QuestionId = _quiz.Questions.First().Id, SelectedOptionPositions = new List<int> { 1 } };

			Assert.Throws<UnauthorizedActionException>(() => _attemptService.SubmitAnswer(3, answer));
		}

		[Fact]
		public void SubmitAnswer_WithShuffledOptions_IsStillGradedCorrectly()
		{
			Attempt? created = null;
			_attemptRepository.Setup(r => r.AddAttempt(It.IsAny<Attempt>()))
				.Callback<Attempt>(a => { TestData.SetId(a, 77); created = a; });
			_attemptRepository.Setup(r => r.GetAttemptById(77)).Returns(() => created);
			var session = _attemptService.StartAttempt(2, 10);
			var single = session.Questions.Single(q => q.Type == QuestionType.Single);

			// The correct option is "B", wherever the shuffle placed it on screen.
			var chosen = single.Options.Single(o => o.Text == "B");
			_attemptService.SubmitAnswer(2, new SubmitAnswerDTO
			{
				AttemptId = 77,
				QuestionId = single.Id,
				SelectedOptionPositions = new List<int> { chosen.Position }
			});

			Assert.True(created!.Answers.Single().IsCorrect);
			VerifyLogged(LogEventType.AnswerSaved, Times.Once());
		}

		[Fact]
		public void FinishAttempt_SendsTheResultEmailWithCertificateAndLogs()
		{
			var attempt = RunningAttempt(77);
			foreach (var question in _quiz.Questions)
			{
				attempt.SubmitAnswer(question, TestData.CorrectAnswer(question), DateTime.Now);
			}

			var result = _attemptService.FinishAttempt(2, 77);

			Assert.True(result.EmailSent);
			Assert.Equal(AttemptResult.Passed, result.Result);
			Assert.Equal(6, result.Score);
			Assert.Equal(3, result.CorrectCount);
			Assert.NotNull(result.CertificateCode);
			_emailService.Verify(e => e.Send(_student.Email,
				It.Is<string>(subject => subject.Contains(_quiz.Title)),
				It.Is<string>(body => body.Contains(result.CertificateCode!))), Times.Once);
			VerifyLogged(LogEventType.QuizFinished, Times.Once());
			VerifyLogged(LogEventType.EmailSent, Times.Once());
		}

		[Fact]
		public void FinishAttempt_EmailFailure_DoesNotBreakTheFinish()
		{
			RunningAttempt(77);
			_emailService.Setup(e => e.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
				.Throws(new InvalidOperationException("SMTP server is down"));

			var result = _attemptService.FinishAttempt(2, 77);

			Assert.False(result.EmailSent);
			Assert.Equal(AttemptStatus.Finished, result.Status);
			Assert.Equal(3, result.IncorrectCount);
			_unitOfWork.Verify(u => u.SaveChanges(), Times.Once);
			_logger.Verify(l => l.Log(LogEventType.EmailFailed, It.Is<string>(m => m.Contains("SMTP server is down")), 2, 10, 77), Times.Once);
		}

		[Fact]
		public void FinishAttempt_Twice_IsRejected()
		{
			RunningAttempt(77);
			_attemptService.FinishAttempt(2, 77);

			Assert.Throws<InvalidAttemptStateException>(() => _attemptService.FinishAttempt(2, 77));
		}

		[Fact]
		public void CancelAttempt_CancelsAndLogs()
		{
			var attempt = RunningAttempt(77);

			_attemptService.CancelAttempt(2, 77);

			Assert.Equal(AttemptStatus.Cancelled, attempt.Status);
			VerifyLogged(LogEventType.AttemptCancelled, Times.Once());
		}
	}
}
