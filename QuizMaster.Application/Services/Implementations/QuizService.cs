using AutoMapper;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Application.Services.Implementations
{
	public class QuizService : IQuizService
	{
		private readonly IQuizRepository _quizRepository;
		private readonly IUserRepository _userRepository;
		private readonly IMapper _mapper;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAppLogger _logger;

		public QuizService(IQuizRepository quizRepository, IUserRepository userRepository, IMapper mapper,
			IUnitOfWork unitOfWork, IAppLogger logger)
		{
			_quizRepository = quizRepository;
			_userRepository = userRepository;
			_mapper = mapper;
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public List<QuizDTO> GetPublishedQuizzes()
		{
			return _mapper.Map<List<QuizDTO>>(_quizRepository.GetPublishedQuizzes());
		}

		public List<QuizDTO> GetLecturerQuizzes(int lecturerId)
		{
			AccessGuard.RequireLecturer(_userRepository, lecturerId);
			return _mapper.Map<List<QuizDTO>>(_quizRepository.GetQuizzesByLecturer(lecturerId));
		}

		public QuizDetailsDTO GetQuizDetails(int lecturerId, int quizId)
		{
			var quiz = AccessGuard.RequireOwnedQuiz(_userRepository, _quizRepository, lecturerId, quizId);
			return _mapper.Map<QuizDetailsDTO>(quiz);
		}

		public QuizDTO CreateQuiz(int lecturerId, CreateQuizDTO quiz)
		{
			ArgumentNullException.ThrowIfNull(quiz);
			var lecturer = AccessGuard.RequireLecturer(_userRepository, lecturerId);

			var newQuiz = _mapper.Map<Quiz>(quiz);
			newQuiz.Title = (newQuiz.Title ?? string.Empty).Trim();
			newQuiz.Topic = (newQuiz.Topic ?? string.Empty).Trim();
			newQuiz.LecturerId = lecturer.Id;
			newQuiz.Lecturer = lecturer;
			newQuiz.CreatedAt = DateTime.Now;

			var errors = newQuiz.ValidateSettings();
			if (errors.Count > 0)
			{
				throw new DomainValidationException(errors);
			}

			_quizRepository.AddQuiz(newQuiz);
			_unitOfWork.SaveChanges();

			_logger.Log(LogEventType.QuizCreated, $"Quiz '{newQuiz.Title}' was created.", lecturerId, newQuiz.Id);
			return _mapper.Map<QuizDTO>(newQuiz);
		}

		public void UpdateQuiz(int lecturerId, UpdateQuizDTO quiz)
		{
			ArgumentNullException.ThrowIfNull(quiz);
			var existing = AccessGuard.RequireOwnedQuiz(_userRepository, _quizRepository, lecturerId, quiz.Id);
			existing.EnsureEditable();

			_mapper.Map(quiz, existing);
			existing.Title = (existing.Title ?? string.Empty).Trim();
			existing.Topic = (existing.Topic ?? string.Empty).Trim();

			var errors = existing.ValidateSettings();
			if (errors.Count > 0)
			{
				throw new DomainValidationException(errors);
			}

			existing.UpdatedAt = DateTime.Now;
			_quizRepository.UpdateQuiz(existing);
			_unitOfWork.SaveChanges();

			_logger.Log(LogEventType.QuizUpdated, $"Quiz '{existing.Title}' was updated.", lecturerId, existing.Id);
		}

		public void PublishQuiz(int lecturerId, int quizId)
		{
			var quiz = AccessGuard.RequireOwnedQuiz(_userRepository, _quizRepository, lecturerId, quizId);
			quiz.Publish();
			_quizRepository.UpdateQuiz(quiz);
			_unitOfWork.SaveChanges();

			_logger.Log(LogEventType.QuizPublished, $"Quiz '{quiz.Title}' was published.", lecturerId, quiz.Id);
		}

		public void CloseQuiz(int lecturerId, int quizId)
		{
			var quiz = AccessGuard.RequireOwnedQuiz(_userRepository, _quizRepository, lecturerId, quizId);
			quiz.Close();
			_quizRepository.UpdateQuiz(quiz);
			_unitOfWork.SaveChanges();

			_logger.Log(LogEventType.QuizClosed, $"Quiz '{quiz.Title}' was closed.", lecturerId, quiz.Id);
		}

		public void DeleteQuiz(int lecturerId, int quizId)
		{
			var quiz = AccessGuard.RequireOwnedQuiz(_userRepository, _quizRepository, lecturerId, quizId);
			if (_quizRepository.HasAttempts(quiz.Id))
			{
				throw new DomainValidationException("This quiz already has attempts, so it can't be deleted. Close it instead.");
			}

			var title = quiz.Title;
			_quizRepository.DeleteQuiz(quiz);
			_unitOfWork.SaveChanges();

			_logger.Log(LogEventType.QuizDeleted, $"Quiz '{title}' was deleted.", lecturerId, quizId);
		}
	}
}
