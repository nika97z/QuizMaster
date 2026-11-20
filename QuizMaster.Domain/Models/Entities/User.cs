using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.Entities
{
	/// <summary>
	/// Base class of the User → Lecturer / Student hierarchy (stored in one Users table, TPH).
	/// </summary>
	public abstract class User
	{
		public int Id { get; set; }
		public string Username { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		/// <summary>
		/// BCrypt hash. BCrypt stores the salt inside the hash string, so there is no separate salt column.
		/// </summary>
		public string PasswordHash { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; } = DateTime.Now;

		/// <summary>
		/// Each subclass reports its own role (polymorphism). Not a column: the Role column is the TPH discriminator.
		/// </summary>
		public abstract UserRole Role { get; }

		public override string ToString()
		{
			return $"{Username} ({Role})";
		}
	}
}
