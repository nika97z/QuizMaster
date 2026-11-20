namespace QuizMaster.Infrastructure.Ai
{
	/// <summary>
	/// Bound from the "OpenAI" section of appsettings.json. Keep the API key out of source control
	/// (use appsettings.Local.json or the QUIZMASTER_OpenAI__ApiKey environment variable).
	/// </summary>
	public class OpenAiSettings
	{
		public string ApiKey { get; set; } = string.Empty;
		public string Model { get; set; } = "gpt-5.4-mini";
		public string BaseUrl { get; set; } = "https://api.openai.com/v1";
		public int TimeoutSeconds { get; set; } = 120;

		public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
	}
}
