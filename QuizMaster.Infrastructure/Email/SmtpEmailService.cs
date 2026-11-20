using QuizMaster.Domain.Interfaces;
using System.Net;
using System.Net.Mail;

namespace QuizMaster.Infrastructure.Email
{
	/// <summary>
	/// Sends real emails through an SMTP server. Any failure is thrown to the caller, which logs it.
	/// </summary>
	public class SmtpEmailService : IEmailService
	{
		private readonly SmtpSettings _settings;

		public SmtpEmailService(SmtpSettings settings)
		{
			_settings = settings;
		}

		public void Send(string toAddress, string subject, string body)
		{
			var from = string.IsNullOrWhiteSpace(_settings.FromAddress) ? _settings.Username : _settings.FromAddress;
			if (string.IsNullOrWhiteSpace(from))
			{
				throw new InvalidOperationException("SMTP is missing a FromAddress in appsettings.json.");
			}

			using var message = new MailMessage
			{
				From = new MailAddress(from, _settings.FromName),
				Subject = subject,
				Body = body,
				IsBodyHtml = false
			};
			message.To.Add(new MailAddress(toAddress));

			using var client = new SmtpClient(_settings.Host, _settings.Port)
			{
				EnableSsl = _settings.EnableSsl,
				DeliveryMethod = SmtpDeliveryMethod.Network,
				Timeout = 15000
			};
			if (!string.IsNullOrWhiteSpace(_settings.Username))
			{
				client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
			}

			client.Send(message);
		}
	}
}
