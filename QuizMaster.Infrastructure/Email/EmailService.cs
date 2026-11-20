using QuizMaster.Domain.Interfaces;
using System.Net;
using System.Net.Mail;

namespace QuizMaster.Infrastructure.Email
{
	/// <summary>
	/// The assignment's EmailService. With an SMTP Host it sends the email; any failure is thrown to the caller, which logs it.
	/// Without a Host it prints the email to the console, so the app works without an SMTP server.
	/// The properties are filled from the "Smtp" section of appsettings.json. Keep the password out of source control
	/// (use appsettings.Local.json or the QUIZMASTER_Smtp__Password environment variable).
	/// </summary>
	public class EmailService : IEmailService
	{
		public string Host { get; set; } = string.Empty;
		public int Port { get; set; } = 587;
		public bool EnableSsl { get; set; } = true;
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string FromAddress { get; set; } = string.Empty;
		public string FromName { get; set; } = "QuizMaster";

		public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);

		public void Send(string toAddress, string subject, string body)
		{
			if (!IsConfigured)
			{
				PrintToConsole(toAddress, subject, body);
				return;
			}

			var from = string.IsNullOrWhiteSpace(FromAddress) ? Username : FromAddress;
			if (string.IsNullOrWhiteSpace(from))
			{
				throw new InvalidOperationException("SMTP is missing a FromAddress in appsettings.json.");
			}

			using var message = new MailMessage
			{
				From = new MailAddress(from, FromName),
				Subject = subject,
				Body = body,
				IsBodyHtml = false
			};
			message.To.Add(new MailAddress(toAddress));

			using var client = new SmtpClient(Host, Port)
			{
				EnableSsl = EnableSsl,
				DeliveryMethod = SmtpDeliveryMethod.Network,
				Timeout = 15000
			};
			if (!string.IsNullOrWhiteSpace(Username))
			{
				client.Credentials = new NetworkCredential(Username, Password);
			}

			client.Send(message);
		}

		// Accepts any address text, so test accounts with made-up emails still see their result email.
		private static void PrintToConsole(string toAddress, string subject, string body)
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
