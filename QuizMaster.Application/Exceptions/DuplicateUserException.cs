namespace QuizMaster.Application.Exceptions
{
	public class DuplicateUserException : Exception
	{
		public DuplicateUserException() : base("This username or email is already registered.")
		{
		}

		public DuplicateUserException(string message) : base(message)
		{
		}
	}
}
