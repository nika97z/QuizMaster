using QuizMaster.Domain.Exceptions;

namespace QuizMaster.Domain.Models.Entities
{
	public class Quiz
	{
		public const int MinTimeLimitMinutes = 1;
		public const int MaxTimeLimitMinutes = 300;
		public const int MinPassPercent = 1;
		public const int MaxPassPercent = 100;

		// Column sizes in the database (see QuizConfiguration). Validation doesn't check them;
		// the input forms and the AI quiz generator keep texts within them.
		public const int MaxTitleLength = 200;
		public const int MaxTopicLength = 100;

		public int Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Topic { get; set; } = string.Empty;
		public int TimeLimitMinutes { get; set; }
		public int PassPercent { get; set; }
		public bool IsPublished { get; private set; }
		public int LecturerId { get; set; }
		public Lecturer Lecturer { get; set; } = null!;
		public DateTime CreatedAt { get; set; } = DateTime.Now;
		public DateTime? UpdatedAt { get; set; }
		public List<Question> Questions { get; set; } = new();
		public List<Attempt> Attempts { get; set; } = new();

		// Calculated from the questions; not stored.
		public int QuestionCount => Questions.Count;
		public int MaxScore => Questions.Sum(q => q.Points);

		public void AddQuestion(Question question)
		{
			ArgumentNullException.ThrowIfNull(question);
			question.QuizId = Id;
			question.OrderIndex = Questions.Count == 0 ? 1 : Questions.Max(q => q.OrderIndex) + 1;
			Questions.Add(question);
		}

		public IReadOnlyList<string> ValidateSettings()
		{
			var errors = new List<string>();
			if (string.IsNullOrWhiteSpace(Title))
			{
				errors.Add("Title can't be empty.");
			}
			if (string.IsNullOrWhiteSpace(Topic))
			{
				errors.Add("Topic can't be empty.");
			}
			if (TimeLimitMinutes < MinTimeLimitMinutes || TimeLimitMinutes > MaxTimeLimitMinutes)
			{
				errors.Add($"Time limit must be between {MinTimeLimitMinutes} and {MaxTimeLimitMinutes} minutes.");
			}
			if (PassPercent < MinPassPercent || PassPercent > MaxPassPercent)
			{
				errors.Add($"Pass percent must be between {MinPassPercent} and {MaxPassPercent}.");
			}
			return errors;
		}

		public IReadOnlyList<string> ValidateForPublish()
		{
			var errors = ValidateSettings().ToList();
			if (Questions.Count == 0)
			{
				errors.Add("An empty quiz can't be published. Add at least one question.");
			}

			var number = 1;
			foreach (var question in Questions.OrderBy(q => q.OrderIndex))
			{
				foreach (var error in question.Validate())
				{
					errors.Add($"Question {number}: {error}");
				}
				number++;
			}
			return errors;
		}

		public void Publish()
		{
			var errors = ValidateForPublish();
			if (errors.Count > 0)
			{
				throw new DomainValidationException(errors);
			}
			IsPublished = true;
			UpdatedAt = DateTime.Now;
		}

		public void Close()
		{
			IsPublished = false;
			UpdatedAt = DateTime.Now;
		}

		public void EnsureEditable()
		{
			if (IsPublished)
			{
				throw new DomainValidationException("This quiz is published. Close it before making changes.");
			}
		}
	}
}
