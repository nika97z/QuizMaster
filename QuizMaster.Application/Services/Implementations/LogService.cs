using AutoMapper;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.LogDTOs;

namespace QuizMaster.Application.Services.Implementations
{
	public class LogService : ILogService
	{
		private readonly IAppLogger _logger;
		private readonly IUserRepository _userRepository;
		private readonly IMapper _mapper;

		public LogService(IAppLogger logger, IUserRepository userRepository, IMapper mapper)
		{
			_logger = logger;
			_userRepository = userRepository;
			_mapper = mapper;
		}

		public List<LogEntryDTO> GetRecentLogs(int lecturerId, int count)
		{
			AccessGuard.RequireLecturer(_userRepository, lecturerId);
			return _mapper.Map<List<LogEntryDTO>>(_logger.GetRecentEntries(count));
		}
	}
}
