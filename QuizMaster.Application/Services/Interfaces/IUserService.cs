using QuizMaster.Domain.Models.DTOs.UserDTOs;

namespace QuizMaster.Application.Services.Interfaces
{
	public interface IUserService
	{
		UserDTO Register(RegisterUserDTO user);
		UserDTO Login(LoginDTO login);
		UserDTO GetUserById(int id);
	}
}
