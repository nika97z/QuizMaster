using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Factories;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuestionDTOs;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Application.Services.Implementations
{
	/// <summary>
	/// Lets the AI write a quiz. The AI writes the title and the questions with their correct answers. Every question
	/// then goes through QuestionFactory, exactly like a lecturer's own; questions that fail are left out. The quiz is
	/// saved as a draft, so the lecturer checks it (and can edit it) before publishing.
	/// </summary>
	public class QuizGenerationService : IQuizGenerationService
	{
		public const int MinQuestions = 1;
		public const int MaxQuestions = 10;
		public const int MaxSubjectLength = Quiz.MaxTopicLength;

		// The AI is asked for 1-3 points per question; anything odd is kept within this range.
		private const int MaxGeneratedPoints = 10;

		/// <summary>
		/// One of these is picked when the lecturer asks for a random subject.
		/// </summary>
		public static readonly IReadOnlyList<string> RandomSubjects = new[]
		{
			"C# programming", "Object-oriented programming", "SQL and relational databases", "Algorithms and data structures",
			"Computer networks", "Operating systems", "Web development", "Cybersecurity basics", "Software testing",
			"Git and version control", "Computer hardware", "Artificial intelligence basics", "World geography", "World history",
			"Astronomy", "Physics", "Chemistry", "Biology", "Human anatomy", "Mathematics", "Statistics", "Economics",
			"World literature", "Music history", "Art history", "Olympic sports"
		};

		private readonly IQuizGenerator _generator;
		private readonly IQuizRepository _quizRepository;
		private readonly IUserRepository _userRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAppLogger _logger;

		public QuizGenerationService(IQuizGenerator generator, IQuizRepository quizRepository, IUserRepository userRepository,
			IUnitOfWork unitOfWork, IAppLogger logger)
		{
			_generator = generator;
			_quizRepository = quizRepository;
			_userRepository = userRepository;
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public bool IsAvailable => _generator.IsConfigured;

		public QuizGenerationResultDTO GenerateQuiz(int lecturerId, GenerateQuizDTO request)
		{
			ArgumentNullException.ThrowIfNull(request);
			var lecturer = AccessGuard.RequireLecturer(_userRepository, lecturerId);
			if (!_generator.IsConfigured)
			{
				throw new QuizGenerationException("AI quiz generation isn't set up. Add an OpenAI API key (OpenAI:ApiKey) to appsettings.Local.json.");
			}

			var subject = request.RandomSubject
				? RandomSubjects[Random.Shared.Next(RandomSubjects.Count)]
				: (request.Subject ?? string.Empty).Trim();
			ValidateRequest(subject, request);

			// Checked before the AI is asked, so a bad request costs nothing.
			var generated = _generator.Generate(subject, request.QuestionCount);

			var quiz = new Quiz
			{
				Title = Fit(generated.Title, Quiz.MaxTitleLength, fallback: $"{Fit(subject, Quiz.MaxTitleLength - 5, subject)} quiz"),
				Topic = Fit(generated.Topic, Quiz.MaxTopicLength, fallback: subject),
				TimeLimitMinutes = request.TimeLimitMinutes,
				PassPercent = request.PassPercent,
				LecturerId = lecturer.Id,
				Lecturer = lecturer,
				CreatedAt = DateTime.Now
			};

			var skipped = 0;
			foreach (var question in generated.Questions.Take(request.QuestionCount))
			{
				if (!TryAddQuestion(quiz, question))
				{
					skipped++;
				}
			}
			if (quiz.Questions.Count == 0)
			{
				throw new QuizGenerationException("The AI didn't write any usable questions. Please try again.");
			}

			_quizRepository.AddQuiz(quiz);
			_unitOfWork.SaveChanges();

			var skippedNote = skipped > 0 ? $" {skipped} unusable question(s) were left out." : string.Empty;
			_logger.Log(LogEventType.QuizGenerated,
				$"Quiz '{quiz.Title}' was written by AI about '{subject}' with {quiz.Questions.Count} questions.{skippedNote}", lecturerId, quiz.Id);

			return new QuizGenerationResultDTO
			{
				QuizId = quiz.Id,
				Title = quiz.Title,
				Subject = subject,
				QuestionCount = quiz.Questions.Count,
				SkippedQuestions = skipped
			};
		}

		private static void ValidateRequest(string subject, GenerateQuizDTO request)
		{
			var errors = new List<string>();
			if (subject.Length == 0)
			{
				errors.Add("Enter a subject, or choose a random subject.");
			}
			else if (subject.Length > MaxSubjectLength)
			{
				errors.Add($"The subject can be at most {MaxSubjectLength} characters long.");
			}
			if (request.QuestionCount < MinQuestions || request.QuestionCount > MaxQuestions)
			{
				errors.Add($"The number of questions must be between {MinQuestions} and {MaxQuestions}.");
			}

			// The time limit and pass percent follow the same rules as any quiz. The AI writes the title and topic later.
			var settings = new Quiz { Title = "-", Topic = "-", TimeLimitMinutes = request.TimeLimitMinutes, PassPercent = request.PassPercent };
			errors.AddRange(settings.ValidateSettings());

			if (errors.Count > 0)
			{
				throw new DomainValidationException(errors);
			}
		}

		/// <summary>
		/// Adds one question from the AI if it fits the database and passes the domain's checks.
		/// </summary>
		private static bool TryAddQuestion(Quiz quiz, CreateQuestionDTO generated)
		{
			var text = generated.Text?.Trim() ?? string.Empty;
			var options = (generated.Options ?? new List<string>()).Select(o => o?.Trim() ?? string.Empty).ToList();
			if (text.Length > Question.MaxTextLength || options.Any(o => o.Length > QuestionOption.MaxTextLength))
			{
				return false;
			}

			try
			{
				var points = Math.Clamp(generated.Points, 1, MaxGeneratedPoints);
				quiz.AddQuestion(QuestionFactory.Create(generated.Type, text, points, options, generated.CorrectOptionIndexes));
				return true;
			}
			catch (DomainValidationException)
			{
				return false;
			}
		}

		private static string Fit(string? text, int maxLength, string fallback)
		{
			var value = text?.Trim();
			if (string.IsNullOrEmpty(value))
			{
				value = fallback;
			}
			return value.Length <= maxLength ? value : value[..(maxLength - 3)].TrimEnd() + "...";
		}
	}
}
