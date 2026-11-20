using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.StatisticsDTOs;

namespace QuizMaster.Web.Pages.Lecturer
{
	public class StatisticsModel : QuizMasterPageModel
	{
		private readonly IStatisticsService _statisticsService;

		public StatisticsModel(IStatisticsService statisticsService, IAppLogger logger) : base(logger)
		{
			_statisticsService = statisticsService;
		}

		public decimal AverageScore { get; private set; }
		public decimal AveragePercent { get; private set; }
		public LeaderboardEntryDTO? BestStudent { get; private set; }
		public List<QuizStatisticsDTO> ByQuiz { get; private set; } = new();
		public List<TopicStatisticsDTO> ByTopic { get; private set; } = new();

		public void OnGet()
		{
			Execute(() =>
			{
				AverageScore = _statisticsService.GetAverageScore();
				AveragePercent = _statisticsService.GetAveragePercent();
				BestStudent = _statisticsService.GetBestStudent();
				ByQuiz = _statisticsService.GetQuizStatistics();
				ByTopic = _statisticsService.GetTopicStatistics();
			});
		}
	}
}
