using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.LogDTOs;

namespace QuizMaster.Web.Pages.Lecturer
{
	public class LogsModel : QuizMasterPageModel
	{
		public const int EntryCount = 30;

		private readonly ILogService _logService;

		public LogsModel(ILogService logService, IAppLogger logger) : base(logger)
		{
			_logService = logService;
		}

		/// <summary>
		/// Newest first.
		/// </summary>
		public List<LogEntryDTO> Entries { get; private set; } = new();

		public void OnGet()
		{
			Execute(() => Entries = _logService.GetRecentLogs(CurrentUserId, EntryCount));
		}
	}
}
