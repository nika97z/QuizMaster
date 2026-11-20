using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.Entities
{
	public class Lecturer : User
	{
		public List<Quiz> Quizzes { get; set; } = new();

		public override UserRole Role => UserRole.Lecturer;
	}
}
