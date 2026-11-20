using QuizMaster.Application.Services.Implementations;
using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Models.DTOs.LogDTOs;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Domain.Models.DTOs.StatisticsDTOs;
using QuizMaster.Domain.Models.DTOs.UserDTOs;
using QuizMaster.UI.Helpers;

namespace QuizMaster.UI.Menus
{
	public class LecturerMenu
	{
		private readonly ServiceRunner _services;
		private readonly SafeExecutor _safe;
		private readonly UserDTO _user;

		public LecturerMenu(ServiceRunner services, SafeExecutor safe, UserDTO user)
		{
			_services = services;
			_safe = safe;
			_user = user;
		}

		public void Run()
		{
			while (true)
			{
				ConsoleHelper.Header($"Lecturer menu - {_user.Username}");
				Console.WriteLine("1) My quizzes");
				Console.WriteLine("2) Create a quiz");
				Console.WriteLine("3) Manage a quiz (details, questions, publish/close, delete)");
				Console.WriteLine("4) Statistics");
				Console.WriteLine("5) Leaderboards");
				Console.WriteLine("6) Recent log entries");
				Console.WriteLine("7) Create a quiz with AI");
				Console.WriteLine("8) Delete a student account");
				Console.WriteLine("0) Log out");

				switch (ConsoleHelper.ReadInt("Choose", 0, 8))
				{
					case 1:
						_safe.Execute(() => ShowMyQuizzes(), _user.Id);
						break;
					case 2:
						_safe.Execute(CreateQuiz, _user.Id);
						break;
					case 3:
						_safe.Execute(ManageQuiz, _user.Id);
						break;
					case 4:
						_safe.Execute(ShowStatistics, _user.Id);
						break;
					case 5:
						_safe.Execute(ShowLeaderboards, _user.Id);
						break;
					case 6:
						_safe.Execute(ShowLogs, _user.Id);
						break;
					case 7:
						_safe.Execute(CreateQuizWithAi, _user.Id);
						break;
					case 8:
						_safe.Execute(DeleteStudent, _user.Id);
						break;
					case 0:
						return;
				}
			}
		}

		private List<QuizDTO> ShowMyQuizzes()
		{
			var quizzes = _services.Get<IQuizService, List<QuizDTO>>(s => s.GetLecturerQuizzes(_user.Id));
			ConsoleHelper.Header("My quizzes");
			ConsoleHelper.PrintTable(
				new[] { "Id", "Title", "Topic", "Questions", "Max score", "Time limit", "Pass %", "Status" },
				quizzes.Select(q => new[]
				{
					q.Id.ToString(), q.Title, q.Topic, q.QuestionCount.ToString(), q.MaxScore.ToString(),
					$"{q.TimeLimitMinutes} min", $"{q.PassPercent}%", q.IsPublished ? "Published" : "Draft/closed"
				}));
			return quizzes;
		}

		private void CreateQuiz()
		{
			ConsoleHelper.Header("Create a quiz");
			var dto = new CreateQuizDTO
			{
				Title = ConsoleHelper.ReadNonEmpty("Title", 200),
				Topic = ConsoleHelper.ReadNonEmpty("Topic", 100),
				TimeLimitMinutes = ConsoleHelper.ReadInt("Time limit in minutes", 1, 300),
				PassPercent = ConsoleHelper.ReadInt("Pass percent", 1, 100)
			};

			var quiz = _services.Get<IQuizService, QuizDTO>(s => s.CreateQuiz(_user.Id, dto));
			ConsoleHelper.Success($"Quiz {quiz.Id} \"{quiz.Title}\" was created as a draft. Add questions, then publish it.");
			if (ConsoleHelper.Confirm("Add questions now"))
			{
				new QuizEditor(_services, _safe, _user, quiz.Id).Run();
			}
		}

		/// <summary>
		/// The AI writes the quiz (title, questions, correct answers) on a subject, or on a random one when the subject
		/// is left empty. The quiz is saved as a draft, so the lecturer checks it before publishing.
		/// </summary>
		private void CreateQuizWithAi()
		{
			ConsoleHelper.Header("Create a quiz with AI");
			if (!_services.Get<IQuizGenerationService, bool>(s => s.IsAvailable))
			{
				ConsoleHelper.Warning("AI quiz generation isn't set up. Add an OpenAI API key (OpenAI:ApiKey) to appsettings.Local.json.");
				return;
			}

			var subject = ConsoleHelper.ReadOptional("Subject (press Enter for a random subject)");
			var request = new GenerateQuizDTO
			{
				Subject = subject,
				RandomSubject = subject.Length == 0,
				QuestionCount = ConsoleHelper.ReadInt("Number of questions", QuizGenerationService.MinQuestions, QuizGenerationService.MaxQuestions),
				TimeLimitMinutes = ConsoleHelper.ReadInt("Time limit in minutes", 1, 300),
				PassPercent = ConsoleHelper.ReadInt("Pass percent", 1, 100)
			};

			ConsoleHelper.Info("The AI is writing the quiz. This can take up to a minute...");
			var result = _services.Get<IQuizGenerationService, QuizGenerationResultDTO>(s => s.GenerateQuiz(_user.Id, request));
			ConsoleHelper.Success($"Quiz {result.QuizId} \"{result.Title}\" about {result.Subject} was created as a draft with {result.QuestionCount} questions. Check them, then publish it.");
			if (result.SkippedQuestions > 0)
			{
				ConsoleHelper.Warning($"{result.SkippedQuestions} of the AI's questions didn't pass the checks, so they were left out.");
			}
			if (ConsoleHelper.Confirm("Open it now"))
			{
				new QuizEditor(_services, _safe, _user, result.QuizId).Run();
			}
		}

		private void ManageQuiz()
		{
			var quizzes = ShowMyQuizzes();
			if (quizzes.Count == 0)
			{
				return;
			}
			var quizId = ConsoleHelper.ReadInt("Quiz id (0 = back)", 0, int.MaxValue);
			if (quizId == 0)
			{
				return;
			}
			if (!quizzes.Any(q => q.Id == quizId))
			{
				ConsoleHelper.Error("You have no quiz with that id.");
				return;
			}
			new QuizEditor(_services, _safe, _user, quizId).Run();
		}

		private void ShowStatistics()
		{
			var averageScore = _services.Get<IStatisticsService, decimal>(s => s.GetAverageScore());
			var averagePercent = _services.Get<IStatisticsService, decimal>(s => s.GetAveragePercent());
			var best = _services.Get<IStatisticsService, LeaderboardEntryDTO?>(s => s.GetBestStudent());
			var byQuiz = _services.Get<IStatisticsService, List<QuizStatisticsDTO>>(s => s.GetQuizStatistics());
			var byTopic = _services.Get<IStatisticsService, List<TopicStatisticsDTO>>(s => s.GetTopicStatistics());

			ConsoleHelper.Header("Statistics (finished attempts)");
			Console.WriteLine($"Average score:   {averageScore}");
			Console.WriteLine($"Average percent: {ConsoleHelper.FormatPercent(averagePercent)}");
			Console.WriteLine(best == null
				? "Best student:    -"
				: $"Best student:    {best.Username} (total best score {best.TotalBestScore}, average {ConsoleHelper.FormatPercent(best.AveragePercent)})");

			ConsoleHelper.Header("Results by quiz");
			ConsoleHelper.PrintTable(
				new[] { "Quiz", "Topic", "Attempts", "Avg score", "Avg %", "Pass rate", "Best student" },
				byQuiz.Select(q => new[]
				{
					q.Title, q.Topic, q.AttemptCount.ToString(), q.AverageScore.ToString(),
					ConsoleHelper.FormatPercent(q.AveragePercent), ConsoleHelper.FormatPercent(q.PassRate), q.BestStudent
				}));

			ConsoleHelper.Header("Results by topic");
			ConsoleHelper.PrintTable(
				new[] { "Topic", "Quizzes", "Attempts", "Avg %", "Pass rate" },
				byTopic.Select(t => new[]
				{
					t.Topic, t.QuizCount.ToString(), t.AttemptCount.ToString(),
					ConsoleHelper.FormatPercent(t.AveragePercent), ConsoleHelper.FormatPercent(t.PassRate)
				}));
		}

		private void ShowLeaderboards()
		{
			LeaderboardViews.ShowOverall(_services);
			var quizzes = _services.Get<IQuizService, List<QuizDTO>>(s => s.GetLecturerQuizzes(_user.Id));
			if (quizzes.Count > 0 && ConsoleHelper.Confirm("Show the leaderboard of one of your quizzes"))
			{
				LeaderboardViews.ChooseQuizAndShow(_services, quizzes);
			}
		}

		private void ShowLogs()
		{
			var logs = _services.Get<ILogService, List<LogEntryDTO>>(s => s.GetRecentLogs(_user.Id, 30));
			ConsoleHelper.Header("Recent log entries (newest first)");
			ConsoleHelper.PrintTable(
				new[] { "Time", "Event", "Message" },
				logs.Select(l => new[] { l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), l.EventType.ToString(), l.Message }),
				maxColumnWidth: 90);
		}

		private void DeleteStudent()
		{
			var students = _services.Get<IUserService, List<UserDTO>>(s => s.GetStudents(_user.Id));
			ConsoleHelper.Header("Students");
			if (students.Count == 0)
			{
				ConsoleHelper.Info("There are no student accounts.");
				return;
			}
			ConsoleHelper.PrintTable(
				new[] { "Id", "Username", "Email" },
				students.Select(s => new[] { s.Id.ToString(), s.Username, s.Email }));

			var studentId = ConsoleHelper.ReadInt("Student id to delete (0 = back)", 0, int.MaxValue);
			if (studentId == 0)
			{
				return;
			}
			var student = students.FirstOrDefault(s => s.Id == studentId);
			if (student == null)
			{
				ConsoleHelper.Error("There is no student with that id.");
				return;
			}
			if (!ConsoleHelper.Confirm($"Delete {student.Username} and all of their attempts permanently"))
			{
				return;
			}

			_services.Do<IUserService>(s => s.DeleteStudent(_user.Id, studentId));
			ConsoleHelper.Success($"The account of {student.Username} was deleted.");
		}
	}
}
