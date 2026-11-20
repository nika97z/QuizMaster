using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.DTOs.UserDTOs
{
	public class UserDTO
	{
		public int Id { get; set; }
		public string Username { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public UserRole Role { get; set; }

		public override string ToString()
		{
			return $"{Id} {Username} {Email} ({Role})";
		}
	}
}
