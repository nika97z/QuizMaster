namespace QuizMaster.Domain.Models.DTOs.AttemptDTOs
{
	public class SubmitAnswerDTO
	{
		public int AttemptId { get; set; }
		public int QuestionId { get; set; }

		// Original option positions (QuestionOptionDTO.Position), not the numbers shown on screen.
		public List<int> SelectedOptionPositions { get; set; } = new();
	}
}
