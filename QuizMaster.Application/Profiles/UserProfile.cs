using AutoMapper;
using QuizMaster.Domain.Models.DTOs.UserDTOs;
using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Application.Profiles
{
	public class UserProfile : Profile
	{
		public UserProfile()
		{
			// Mapped per subclass, so the Role each subclass reports is used.
			CreateMap<User, UserDTO>()
				.Include<Student, UserDTO>()
				.Include<Lecturer, UserDTO>();
			CreateMap<Student, UserDTO>();
			CreateMap<Lecturer, UserDTO>();
		}
	}
}
