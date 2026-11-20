namespace QuizMaster.Domain.Exceptions
{
	/// <summary>
	/// Thrown when data breaks a business rule. Carries every problem found, so the UI can show them all at once.
	/// </summary>
	public class DomainValidationException : Exception
	{
		public IReadOnlyList<string> Errors { get; }

		public DomainValidationException(string error) : base(error)
		{
			Errors = new[] { error };
		}

		public DomainValidationException(IEnumerable<string> errors)
			: this(errors.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList())
		{
		}

		private DomainValidationException(List<string> errors)
			: base(errors.Count == 0 ? "Validation failed." : string.Join(" ", errors))
		{
			Errors = errors.Count == 0 ? new[] { "Validation failed." } : errors;
		}
	}

	/// <summary>
	/// Thrown when an attempt is asked to do something its current status doesn't allow.
	/// </summary>
	public class InvalidAttemptStateException : InvalidOperationException
	{
		public InvalidAttemptStateException(string message) : base(message)
		{
		}
	}

	/// <summary>
	/// Thrown when an answer arrives after the attempt's deadline.
	/// </summary>
	public class TimeLimitExceededException : InvalidAttemptStateException
	{
		public TimeLimitExceededException() : base("Time is up. Answers are no longer accepted.")
		{
		}
	}

	/// <summary>
	/// Thrown when the AI can't write a quiz: it isn't set up, the AI service failed, or its answer was unusable.
	/// The message is written for the user.
	/// </summary>
	public class QuizGenerationException : Exception
	{
		public QuizGenerationException(string message) : base(message)
		{
		}

		public QuizGenerationException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
