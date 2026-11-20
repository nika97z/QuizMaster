using QuizMaster.Domain.Interfaces;

namespace QuizMaster.Infrastructure.Email
{
	/// <summary>
	/// Used when SMTP isn't configured: prints the email to the console so the app still works.
	/// It accepts any address text, so test accounts with made-up emails still see their result email.
	/// </summary>
	public class ConsoleEmailService : IEmailService
	{
		public void Send(string toAddress, string subject, string body)
		{
			var previous = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.DarkCyan;
			Console.WriteLine();
			Console.WriteLine("----- Email (SMTP is not configured, so it is shown here) -----");
			Console.WriteLine($"To:      {toAddress}");
			Console.WriteLine($"Subject: {subject}");
			Console.WriteLine();
			Console.WriteLine(body);
			Console.WriteLine("---------------------------------------------------------------");
			Console.ForegroundColor = previous;
		}
	}
}
