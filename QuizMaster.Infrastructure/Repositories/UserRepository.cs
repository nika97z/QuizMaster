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
	}
}
