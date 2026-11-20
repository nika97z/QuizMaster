using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Domain.Models.DTOs.StatisticsDTOs;
using QuizMaster.Web.Helpers;
using System.Globalization;

namespace QuizMaster.Web.Pages
{
	/// <summary>
	/// The overall leaderboard and one quiz's leaderboard. Open to any logged-in user.
	/// </summary>
	public class LeaderboardsModel : QuizMasterPageModel
	{
		private readonly IStatisticsService _statisticsService;
		private readonly IQuizService _quizService;

		public LeaderboardsModel(IStatisticsService statisticsService, IQuizService quizService, IAppLogger logger) : base(logger)
		{
			_statisticsService = statisticsService;
			_quizService = quizService;
		}

		/// <summary>
		/// The quiz chosen in the picker (GET, so the page only reads).
		/// </summary>
		[BindProperty(SupportsGet = true)]
		public int? QuizId { get; set; }

		public List<LeaderboardEntryDTO> Overall { get; private set; } = new();
		public List<QuizDTO> QuizChoices { get; private set; } = new();
		public QuizDTO? SelectedQuiz { get; private set; }
		public List<QuizLeaderboardEntryDTO> QuizEntries { get; private set; } = new();

		public List<SelectListItem> QuizItems => QuizChoices
			.Select(q => new SelectListItem($"{q.Title} ({q.Topic})", q.Id.ToString(CultureInfo.InvariantCulture)))
			.ToList();

		public void OnGet()
		{
			// Students pick from the published quizzes, lecturers from their own quizzes, as in the console.
			var loaded = Execute(() =>
			{
				Overall = _statisticsService.GetOverallLeaderboard();
				QuizChoices = User.IsLecturer()
					? _quizService.GetLecturerQuizzes(CurrentUserId)
					: _quizService.GetPublishedQuizzes();
			});
			if (!loaded || QuizId == null)
			{
				return;
			}

			// GetQuizLeaderboard doesn't check access and the id comes from the URL, so only a quiz from the list is shown.
			var quiz = QuizChoices.FirstOrDefault(q => q.Id == QuizId.Value);
			if (quiz == null)
			{
				ModelState.AddModelError(string.Empty, "There is no quiz with that id.");
				return;
			}

			SelectedQuiz = quiz;
			var quizId = quiz.Id;
			Execute(() => QuizEntries = _statisticsService.GetQuizLeaderboard(quizId));
		}
	}
}
