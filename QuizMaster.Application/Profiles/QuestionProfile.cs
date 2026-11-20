using AutoMapper;
using QuizMaster.Domain.Models.DTOs.QuestionDTOs;
using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Application.Profiles
{
	public class QuestionProfile : Profile
	{
		public QuestionProfile()
		{
			CreateMap<QuestionOption, QuestionOptionDTO>();

			// The correct answer is a private field, so it can never end up in a DTO.
			CreateMap<Question, QuestionDTO>()
				.ForMember(d => d.Options, o => o.MapFrom(s => s.Options.OrderBy(x => x.Position)))
				.ForMember(d => d.DisplayText, o => o.MapFrom(s => s.DisplayQuestion(null)))
				.Include<SingleChoiceQuestion, QuestionDTO>()
				.Include<MultiChoiceQuestion, QuestionDTO>()
				.Include<TrueFalseQuestion, QuestionDTO>();
			CreateMap<SingleChoiceQuestion, QuestionDTO>();
			CreateMap<MultiChoiceQuestion, QuestionDTO>();
			CreateMap<TrueFalseQuestion, QuestionDTO>();
		}
	}
}
