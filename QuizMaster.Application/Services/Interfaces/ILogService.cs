using QuizMaster.Domain.Models.DTOs.LogDTOs;

namespace QuizMaster.Application.Services.Interfaces
{
	public interface ILogService
	{
		List<LogEntryDTO> GetRecentLogs(int lecturerId, int count);
	}
}
