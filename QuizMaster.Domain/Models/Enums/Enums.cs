namespace QuizMaster.Domain.Models.Enums
{
	public enum UserRole
	{
		Student,
		Lecturer
	}

	public enum QuestionType
	{
		Single,
		Multi,
		TrueFalse
	}

	public enum AttemptStatus
	{
		InProgress,
		Finished,
		Cancelled
	}

	public enum AttemptResult
	{
		None,
		Passed,
		Failed
	}

	public enum LogEventType
	{
		UserRegistered,
		LoginSucceeded,
		LoginFailed,
		QuizCreated,
		QuizUpdated,
		QuizPublished,
		QuizClosed,
		QuizDeleted,
		QuestionAdded,
		QuestionUpdated,
		QuestionRemoved,
		QuizStarted,
		AnswerSaved,
		QuizFinished,
		AttemptCancelled,
		AttemptTimedOut,
		EmailSent,
		EmailFailed,
		Error,
		QuizGenerated,
		StudentDeleted
	}
}
