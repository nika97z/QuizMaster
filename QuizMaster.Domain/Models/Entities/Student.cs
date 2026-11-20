using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.Entities
{
	public class Student : User
	{
		public List<Attempt> Attempts { get; set; } = new();

		public override UserRole Role => UserRole.Student;
	}
}
