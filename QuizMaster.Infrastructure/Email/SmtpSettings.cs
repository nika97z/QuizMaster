namespace QuizMaster.Infrastructure.Email
{
	/// <summary>
	/// Bound from the "Smtp" section of appsettings.json. Keep the password out of source control
	/// (use appsettings.Local.json or the QUIZMASTER_Smtp__Password environment variable).
	/// </summary>
	public class SmtpSettings
	{
		public string Host { get; set; } = string.Empty;
		public int Port { get; set; } = 587;
		public bool EnableSsl { get; set; } = true;
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string FromAddress { get; set; } = string.Empty;
		public string FromName { get; set; } = "QuizMaster";

		public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
	}
}
