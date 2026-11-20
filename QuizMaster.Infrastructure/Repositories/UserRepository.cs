using Microsoft.EntityFrameworkCore;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Infrastructure.Data;

namespace QuizMaster.Infrastructure.Repositories
{
	public class UserRepository : IUserRepository
	{
		private readonly QuizMasterDbContext _context;

		public UserRepository(QuizMasterDbContext context)
		{
			_context = context;
		}

		public void AddUser(User user)
		{
			_context.Users.Add(user);
		}

		public User? GetUserById(int id)
		{
			return _context.Users.Find(id);
		}

		// Usernames and emails are compared case-insensitively.
		public User? GetUserByUsername(string username)
		{
			var normalized = (username ?? string.Empty).Trim().ToLower();
			return _context.Users.FirstOrDefault(u => u.Username.ToLower() == normalized);
		}

		public User? GetUserByEmail(string email)
		{
			var normalized = (email ?? string.Empty).Trim().ToLower();
			return _context.Users.FirstOrDefault(u => u.Email.ToLower() == normalized);
		}

		public List<Student> GetAllStudents()
		{
			return _context.Users.OfType<Student>().OrderBy(s => s.Username).ToList();
		}

		// Only finds students, so a lecturer's id returns null. Loads the attempts and their answers for DeleteStudent.
		public Student? GetStudentWithAttempts(int studentId)
		{
			return _context.Users.OfType<Student>()
				.Include(s => s.Attempts)
				.ThenInclude(a => a.Answers)
				.FirstOrDefault(s => s.Id == studentId);
		}

		// The database doesn't allow deleting a student who still has attempts (Restrict), so the attempts go first.
		// Their answers are deleted with them (cascade).
		public void DeleteStudent(Student student)
		{
			_context.Attempts.RemoveRange(student.Attempts);
			_context.Users.Remove(student);
		}
	}
}
