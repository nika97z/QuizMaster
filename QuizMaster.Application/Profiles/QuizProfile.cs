using AutoMapper;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Application.Profiles
{
	public class QuizProfile : Profile
	{
		public QuizProfile()
		{
			CreateMap<Quiz, QuizDTO>()
				.ForMember(d => d.LecturerName, o => o.MapFrom(s => s.Lecturer.Username));
			CreateMap<Quiz, QuizDetailsDTO>()
				.IncludeBase<Quiz, QuizDTO>()
				.ForMember(d => d.Questions, o => o.MapFrom(s => s.Questions.OrderBy(q => q.OrderIndex)));

			CreateMap<CreateQuizDTO, Quiz>();
			CreateMap<UpdateQuizDTO, Quiz>()
				.ForMember(d => d.Id, o => o.Ignore());
		}
	}
}
