using QuizMaster.Domain.Models.DTOs.UserDTOs;

namespace QuizMaster.Application.Services.Interfaces
{
	public interface IUserService
	{
		UserDTO Register(RegisterUserDTO user);
		UserDTO Login(LoginDTO login);
		UserDTO GetUserById(int id);

		/// <summary>
		/// Lecturers only.
		/// </summary>
		List<UserDTO> GetStudents(int lecturerId);

		/// <summary>
		/// Lecturers only. Deletes the student's account together with their attempts and answers. The log keeps its rows.
		/// </summary>
		void DeleteStudent(int lecturerId, int studentId);
	}
}
