using AutoMapper;
using QuizMaster.Domain.Models.DTOs.AttemptDTOs;
using QuizMaster.Domain.Models.DTOs.LogDTOs;
using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Application.Profiles
{
	public class AttemptProfile : Profile
	{
		public AttemptProfile()
		{
			CreateMap<Attempt, AttemptDTO>()
				.ForMember(d => d.QuizTitle, o => o.MapFrom(s => s.Quiz.Title));
			CreateMap<LogEntry, LogEntryDTO>();
		}
	}
}
