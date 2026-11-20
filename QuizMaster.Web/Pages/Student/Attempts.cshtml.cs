using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.AttemptDTOs;

namespace QuizMaster.Web.Pages.Student
{
	public class AttemptsModel : QuizMasterPageModel
	{
		private readonly IAttemptService _attemptService;

		public AttemptsModel(IAttemptService attemptService, IAppLogger logger) : base(logger)
		{
			_attemptService = attemptService;
		}

		public List<AttemptDTO> Attempts { get; private set; } = new();

		public void OnGet()
		{
			Execute(() => Attempts = _attemptService.GetStudentAttempts(CurrentUserId));
		}
	}
}
