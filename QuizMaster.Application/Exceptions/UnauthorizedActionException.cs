namespace QuizMaster.Application.Exceptions
{
	public class UnauthorizedActionException : Exception
	{
		public UnauthorizedActionException() : base("You are not allowed to do this.")
		{
		}

		public UnauthorizedActionException(string message) : base(message)
		{
		}
	}
}
