namespace QuizMaster.UI.Helpers
{
	/// <summary>
	/// Thrown when the input stream ends (for example, piped input runs out), so the app can exit cleanly.
	/// </summary>
	public class EndOfInputException : Exception
	{
		public EndOfInputException() : base("Input ended.")
		{
		}
	}
}
