using QuizMaster.Domain.Models.DTOs.QuizDTOs;

namespace QuizMaster.Application.Services.Interfaces
{
	public interface IQuizGenerationService
	{
		/// <summary>
		/// True when an AI model is set up (an OpenAI API key is configured).
		/// </summary>
		bool IsAvailable { get; }

		QuizGenerationResultDTO GenerateQuiz(int lecturerId, GenerateQuizDTO request);
	}
}
