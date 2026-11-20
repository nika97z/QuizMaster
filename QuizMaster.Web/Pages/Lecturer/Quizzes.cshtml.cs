using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;

namespace QuizMaster.Web.Pages.Lecturer
{
	public class QuizzesModel : QuizMasterPageModel
	{
		private readonly IQuizService _quizService;

		public QuizzesModel(IQuizService quizService, IAppLogger logger) : base(logger)
		{
			_quizService = quizService;
		}

		public List<QuizDTO> Quizzes { get; private set; } = new();

		public void OnGet()
		{
			Execute(() => Quizzes = _quizService.GetLecturerQuizzes(CurrentUserId));
		}
	}
}
