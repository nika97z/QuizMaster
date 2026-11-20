using AutoMapper;
using QuizMaster.Application.Exceptions;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.AttemptDTOs;
using QuizMaster.Domain.Models.DTOs.QuestionDTOs;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Application.Services.Implementations
{
	public class AttemptService : IAttemptService
	{
		private readonly IAttemptRepository _attemptRepository;
		private readonly IQuizRepository _quizRepository;
		private readonly IUserRepository _userRepository;
		private readonly IMapper _mapper;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAppLogger _logger;
		private readonly IEmailService _emailService;

		public AttemptService(IAttemptRepository attemptRepository, IQuizRepository quizRepository,
			IUserRepository userRepository, IMapper mapper, IUnitOfWork unitOfWork, IAppLogger logger,
			IEmailService emailService)
		{
			_attemptRepository = attemptRepository;
			_quizRepository = quizRepository;
			_userRepository = userRepository;
			_mapper = mapper;
			_unitOfWork = unitOfWork;
			_logger = logger;
			_emailService = emailService;
		}

		public AttemptSessionDTO StartAttempt(int studentId, int quizId)
		{
			var student = AccessGuard.RequireStudent(_userRepository, studentId);
			AccessGuard.RequireValidId(quizId, "Quiz id");
			var quiz = _quizRepository.GetQuizWithQuestions(quizId)
				?? throw new NotFoundException($"Quiz {quizId} was not found.");
			if (!quiz.IsPublished)
			{
				throw new DomainValidationException("This quiz is not available right now.");
			}

			var now = DateTime.Now;
			var active = _attemptRepository.GetActiveAttempt(studentId, quizId);
			if (active != null)
			{
				// Only one active attempt per student per quiz: continue it while there is time left.
				if (!active.IsExpired(now))
				{
					return BuildSession(active, quiz, resumed: true);
				}

				active.Cancel(now);
				_attemptRepository.UpdateAttempt(active);
				_unitOfWork.SaveChanges();
				_logger.Log(LogEventType.AttemptTimedOut,
					$"Attempt {active.Id} ran out of time without being finished and was cancelled.", studentId, quizId, active.Id);
			}

			// Bonus: random question order. Grading stays correct because answers refer to question ids and option positions.
			var questionOrder = quiz.Questions.Select(q => q.Id).ToArray();
			Random.Shared.Shuffle(questionOrder);

			var attempt = new Attempt(quiz, student);
			attempt.Start(now, questionOrder);
			_attemptRepository.AddAttempt(attempt);
			_unitOfWork.SaveChanges();

			_logger.Log(LogEventType.QuizStarted, $"'{student.Username}' started '{quiz.Title}'.", studentId, quizId, attempt.Id);
			return BuildSession(attempt, quiz, resumed: false);
		}

		/// <summary>
		/// Reads an attempt that is in progress: its deadline and the questions still to answer. It writes and logs
		/// nothing, so the web app can call it on every page load. StartAttempt can't be used for that, because it
		/// cancels an expired attempt and starts a new one.
		/// </summary>
		public AttemptSessionDTO GetAttemptSession(int studentId, int attemptId)
		{
			var attempt = GetOwnedAttempt(studentId, attemptId);
			if (attempt.Status != AttemptStatus.InProgress)
			{
				throw new InvalidAttemptStateException(
					$"This attempt is already {attempt.Status.ToString().ToLowerInvariant()}, so it can't be continued.");
			}
			return BuildSession(attempt, attempt.Quiz, resumed: false);
		}

		public void SubmitAnswer(int studentId, SubmitAnswerDTO answer)
		{
			ArgumentNullException.ThrowIfNull(answer);
			var attempt = GetOwnedAttempt(studentId, answer.AttemptId);
			var question = attempt.Quiz.Questions.FirstOrDefault(q => q.Id == answer.QuestionId)
				?? throw new NotFoundException($"Question {answer.QuestionId} is not part of this attempt.");

			try
			{
				attempt.SubmitAnswer(question, answer.SelectedOptionPositions ?? new List<int>(), DateTime.Now);
			}
			catch (TimeLimitExceededException)
			{
				_logger.Log(LogEventType.AttemptTimedOut,
					$"Attempt {attempt.Id}: an answer arrived after the time limit.", studentId, attempt.QuizId, attempt.Id);
				throw;
			}

			_attemptRepository.UpdateAttempt(attempt);
			_unitOfWork.SaveChanges();
			_logger.Log(LogEventType.AnswerSaved,
				$"Attempt {attempt.Id}: answer saved for question {question.Id}.", studentId, attempt.QuizId, attempt.Id);
		}

		public AttemptResultDTO FinishAttempt(int studentId, int attemptId)
		{
			var attempt = GetOwnedAttempt(studentId, attemptId);
			attempt.Finish(attempt.Quiz.PassPercent, DateTime.Now);
			_attemptRepository.UpdateAttempt(attempt);
			_unitOfWork.SaveChanges();

			var result = BuildResult(attempt);
			var timeNote = result.TimedOut ? " (time limit reached)" : string.Empty;
			_logger.Log(LogEventType.QuizFinished,
				$"Attempt {attempt.Id} finished: {attempt.Score}/{attempt.MaxScore} ({attempt.Percent}%), {attempt.Result}{timeNote}.",
				studentId, attempt.QuizId, attempt.Id);

			result.EmailSent = TrySendResultEmail(attempt);
			return result;
		}

		public void CancelAttempt(int studentId, int attemptId)
		{
			var attempt = GetOwnedAttempt(studentId, attemptId);
			attempt.Cancel(DateTime.Now);
			_attemptRepository.UpdateAttempt(attempt);
			_unitOfWork.SaveChanges();

			_logger.Log(LogEventType.AttemptCancelled,
				$"Attempt {attempt.Id} was cancelled by the student.", studentId, attempt.QuizId, attempt.Id);
		}

		public List<AttemptDTO> GetStudentAttempts(int studentId)
		{
			AccessGuard.RequireStudent(_userRepository, studentId);
			return _mapper.Map<List<AttemptDTO>>(_attemptRepository.GetAttemptsByStudent(studentId));
		}

		public AttemptResultDTO GetAttemptResult(int studentId, int attemptId)
		{
			var attempt = GetOwnedAttempt(studentId, attemptId);
			if (attempt.Status != AttemptStatus.Finished)
			{
				throw new DomainValidationException("Results are available only for finished attempts.");
			}
			return BuildResult(attempt);
		}

		private Attempt GetOwnedAttempt(int studentId, int attemptId)
		{
			AccessGuard.RequireStudent(_userRepository, studentId);
			AccessGuard.RequireValidId(attemptId, "Attempt id");
			var attempt = _attemptRepository.GetAttemptById(attemptId)
				?? throw new NotFoundException($"Attempt {attemptId} was not found.");
			if (attempt.StudentId != studentId)
			{
				throw new UnauthorizedActionException("This attempt belongs to another student.");
			}
			return attempt;
		}

		private AttemptSessionDTO BuildSession(Attempt attempt, Quiz quiz, bool resumed)
		{
			var questionsById = quiz.Questions.ToDictionary(q => q.Id);
			var answeredIds = attempt.Answers.Select(a => a.QuestionId).ToHashSet();
			var order = attempt.GetQuestionOrder().Where(questionsById.ContainsKey).ToList();

			var session = new AttemptSessionDTO
			{
				AttemptId = attempt.Id,
				QuizId = quiz.Id,
				QuizTitle = quiz.Title,
				StartedAt = attempt.StartedAt,
				Deadline = attempt.Deadline,
				TotalQuestions = order.Count,
				AnsweredCount = order.Count(answeredIds.Contains),
				Resumed = resumed
			};

			foreach (var questionId in order.Where(id => !answeredIds.Contains(id)))
			{
				session.Questions.Add(ToShuffledDto(questionsById[questionId]));
			}
			return session;
		}

		private QuestionDTO ToShuffledDto(Question question)
		{
			var dto = _mapper.Map<QuestionDTO>(question);
			var positions = dto.Options.Select(o => o.Position).ToArray();

			// Bonus: random option order. True/False keeps its natural order.
			if (question.Type != QuestionType.TrueFalse)
			{
				Random.Shared.Shuffle(positions);
			}

			dto.Options = positions.Select(p => dto.Options.First(o => o.Position == p)).ToList();
			dto.DisplayText = question.DisplayQuestion(positions);
			return dto;
		}

		private AttemptResultDTO BuildResult(Attempt attempt)
		{
			var quiz = attempt.Quiz;
			var questionsById = quiz.Questions.ToDictionary(q => q.Id);
			var order = attempt.GetQuestionOrder().Where(questionsById.ContainsKey).ToList();
			if (order.Count == 0)
			{
				order = quiz.Questions.OrderBy(q => q.OrderIndex).Select(q => q.Id).ToList();
			}
			var answers = attempt.Answers.ToDictionary(a => a.QuestionId);

			var review = new List<AnswerReviewDTO>();
			foreach (var questionId in order)
			{
				var question = questionsById[questionId];
				answers.TryGetValue(questionId, out var record);
				review.Add(new AnswerReviewDTO
				{
					Number = review.Count + 1,
					QuestionText = question.Text,
					YourAnswer = record == null ? "(no answer)" : DescribeSelection(question, record.GetSelectedIndexes()),
					IsCorrect = record?.IsCorrect ?? false,
					PointsAwarded = record?.PointsAwarded ?? 0,
					Points = question.Points
				});
			}

			var correctCount = review.Count(r => r.IsCorrect);
			return new AttemptResultDTO
			{
				AttemptId = attempt.Id,
				QuizId = quiz.Id,
				QuizTitle = quiz.Title,
				Score = attempt.Score,
				MaxScore = attempt.MaxScore,
				Percent = attempt.Percent,
				PassPercent = quiz.PassPercent,
				CorrectCount = correctCount,
				IncorrectCount = review.Count - correctCount,
				TotalQuestions = review.Count,
				Result = attempt.Result,
				Status = attempt.Status,
				CertificateCode = attempt.CertificateCode,
				StartedAt = attempt.StartedAt,
				FinishedAt = attempt.FinishedAt,
				TimedOut = attempt.FinishedAt > attempt.Deadline,
				Review = review
			};
		}

		private static string DescribeSelection(Question question, IReadOnlyList<int> positions)
		{
			return string.Join(", ", positions.Select(p =>
				question.Options.FirstOrDefault(o => o.Position == p)?.Text ?? $"option {p + 1}"));
		}

		private bool TrySendResultEmail(Attempt attempt)
		{
			try
			{
				_emailService.Send(attempt.Student.Email,
					ResultEmailBuilder.BuildSubject(attempt),
					ResultEmailBuilder.BuildBody(attempt));
				_logger.Log(LogEventType.EmailSent,
					$"Result email for attempt {attempt.Id} sent to {attempt.Student.Email}.", attempt.StudentId, attempt.QuizId, attempt.Id);
				return true;
			}
			catch (Exception ex)
			{
				// An email problem must never undo or block a finished attempt.
				_logger.Log(LogEventType.EmailFailed,
					$"Result email for attempt {attempt.Id} failed: {ex.GetBaseException().Message}", attempt.StudentId, attempt.QuizId, attempt.Id);
				return false;
			}
		}
	}
}
