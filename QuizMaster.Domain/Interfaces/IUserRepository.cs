using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Domain.Interfaces
{
	public interface IUserRepository
	{
		void AddUser(User user);
		User? GetUserById(int id);
		User? GetUserByUsername(string username);
		User? GetUserByEmail(string email);
		List<Student> GetAllStudents();
		Student? GetStudentWithAttempts(int studentId);
		void DeleteStudent(Student student);
	}
}
