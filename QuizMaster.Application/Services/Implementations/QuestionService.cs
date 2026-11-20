using AutoMapper;
using QuizMaster.Application.Exceptions;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Factories;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuestionDTOs;
using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Application.Services.Implementations
{
	public class QuestionService : IQuestionService
	{
		private readonly IQuestionRepository _questionRepository;
		private readonly IQuizRepository _quizRepository;
		private readonly IUserRepository _userRepository;
		private readonly IMapper _mapper;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAppLogger _logger;

		public QuestionService(IQuestionRepository questionRepository, IQuizRepository quizRepository,
			IUserRepository userRepository, IMapper mapper, IUnitOfWork unitOfWork, IAppLogger logger)
		{
			_questionRepository = questionRepository;
			_quizRepository = quizRepository;
			_userRepository = userRepository;
			_mapper = mapper;
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public QuestionDTO AddQuestion(int lecturerId, CreateQuestionDTO question)
		{
			ArgumentNullException.ThrowIfNull(question);
			var quiz = AccessGuard.RequireOwnedQuiz(_userRepository, _quizRepository, lecturerId, question.QuizId);
			quiz.EnsureEditable();

			// The factory builds the right subclass and validates it (type rules, text, points, options).
			var newQuestion = QuestionFactory.Create(question.Type, question.Text, question.Points,
				question.Options, question.CorrectOptionIndexes);
			quiz.AddQuestion(newQuestion);
			_questionRepository.AddQuestion(newQuestion);
			_unitOfWork.SaveChanges();

			_logger.Log(LogEventType.QuestionAdded, $"A {newQuestion.Type} question was added to '{quiz.Title}'.", lecturerId, quiz.Id);
			return _mapper.Map<QuestionDTO>(newQuestion);
		}

		public QuestionDTO UpdateQuestion(int lecturerId, UpdateQuestionDTO question)
		{
			ArgumentNullException.ThrowIfNull(question);
			AccessGuard.RequireValidId(question.Id, "Question id");
			var existing = _questionRepository.GetQuestionById(question.Id)
				?? throw new NotFoundException($"Question {question.Id} was not found.");
			var quiz = AccessGuard.RequireOwnedQuiz(_userRepository, _quizRepository, lecturerId, existing.QuizId);
			quiz.EnsureEditable();

			if (question.Type != existing.Type)
			{
				throw new DomainValidationException("A question's type can't be changed. Remove it and add a new question instead.");
			}

			QuestionFactory.ApplyUpdate(existing, question.Text, question.Points, question.Options, question.CorrectOptionIndexes);
			_questionRepository.UpdateQuestion(existing);
			_unitOfWork.SaveChanges();

			_logger.Log(LogEventType.QuestionUpdated, $"Question {existing.Id} in '{quiz.Title}' was updated.", lecturerId, quiz.Id);
			return _mapper.Map<QuestionDTO>(existing);
		}

		public void RemoveQuestion(int lecturerId, int questionId)
		{
			AccessGuard.RequireValidId(questionId, "Question id");
			var existing = _questionRepository.GetQuestionById(questionId)
				?? throw new NotFoundException($"Question {questionId} was not found.");
			var quiz = AccessGuard.RequireOwnedQuiz(_userRepository, _quizRepository, lecturerId, existing.QuizId);
			quiz.EnsureEditable();

			if (_questionRepository.HasAnswers(questionId))
			{
				throw new DomainValidationException("This question was answered in past attempts, so it can't be removed.");
			}

			_questionRepository.DeleteQuestion(existing);
			_unitOfWork.SaveChanges();

			_logger.Log(LogEventType.QuestionRemoved, $"Question {questionId} was removed from '{quiz.Title}'.", lecturerId, quiz.Id);
		}
	}
}
