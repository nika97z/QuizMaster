namespace QuizMaster.Domain.Interfaces
{
	/// <summary>
	/// The "EmailService" of the assignment. Throws when the email can't be sent.
	/// </summary>
	public interface IEmailService
	{
		void Send(string toAddress, string subject, string body);
	}
}
