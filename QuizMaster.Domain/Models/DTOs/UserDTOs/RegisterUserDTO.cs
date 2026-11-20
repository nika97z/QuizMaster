using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Domain.Models.DTOs.UserDTOs
{
	public class RegisterUserDTO
	{
		public string Username { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public UserRole Role { get; set; } = UserRole.Student;
	}
}
