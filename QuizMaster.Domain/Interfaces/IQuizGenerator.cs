using QuizMaster.Domain.Models.DTOs.QuizDTOs;

namespace QuizMaster.Domain.Interfaces
{
	/// <summary>
	/// Writes a quiz with an AI model: a title, a topic, and questions with their correct answers.
	/// When it can't, it throws QuizGenerationException with a message the user can read.
	/// </summary>
	public interface IQuizGenerator
	{
		/// <summary>
		/// False when no AI model is set up (no API key).
		/// </summary>
		bool IsConfigured { get; }

		GeneratedQuizDTO Generate(string subject, int questionCount);
	}
}
