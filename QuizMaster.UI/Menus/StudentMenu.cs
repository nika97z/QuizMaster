using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Exceptions;
using QuizMaster.Domain.Models.DTOs.AttemptDTOs;
using QuizMaster.Domain.Models.DTOs.QuestionDTOs;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Domain.Models.DTOs.UserDTOs;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.UI.Helpers;

namespace QuizMaster.UI.Menus
{
	public class StudentMenu
	{
		private enum AnswerOutcome
		{
			Answered,
			Cancelled,
			TimeUp
		}

		private readonly ServiceRunner _services;
		private readonly SafeExecutor _safe;
		private readonly UserDTO _user;

		public StudentMenu(ServiceRunner services, SafeExecutor safe, UserDTO user)
		{
			_services = services;
			_safe = safe;
			_user = user;
		}

		public void Run()
		{
			while (true)
			{
				ConsoleHelper.Header($"Student menu - {_user.Username}");
				Console.WriteLine("1) Quiz catalog");
				Console.WriteLine("2) Take a quiz");
				Console.WriteLine("3) My attempts");
				Console.WriteLine("4) Overall leaderboard");
				Console.WriteLine("5) Quiz leaderboard");
				Console.WriteLine("0) Log out");

				switch (ConsoleHelper.ReadInt("Choose", 0, 5))
				{
					case 1:
						_safe.Execute(() => ShowCatalog(), _user.Id);
						break;
					case 2:
						_safe.Execute(TakeQuiz, _user.Id);
						break;
					case 3:
						_safe.Execute(ShowMyAttempts, _user.Id);
						break;
					case 4:
						_safe.Execute(() => LeaderboardViews.ShowOverall(_services), _user.Id);
						break;
					case 5:
						_safe.Execute(() => LeaderboardViews.ChooseQuizAndShow(_services,
							_services.Get<IQuizService, List<QuizDTO>>(s => s.GetPublishedQuizzes())), _user.Id);
						break;
					case 0:
						return;
				}
			}
		}

		private List<QuizDTO> ShowCatalog()
		{
			var quizzes = _services.Get<IQuizService, List<QuizDTO>>(s => s.GetPublishedQuizzes());
			ConsoleHelper.Header("Quiz catalog");
			ConsoleHelper.PrintTable(
				new[] { "Id", "Title", "Topic", "Questions", "Max score", "Time limit", "Pass %" },
				quizzes.Select(q => new[]
				{
					q.Id.ToString(), q.Title, q.Topic, q.QuestionCount.ToString(), q.MaxScore.ToString(),
					$"{q.TimeLimitMinutes} min", $"{q.PassPercent}%"
				}));
			return quizzes;
		}

		private void TakeQuiz()
		{
			var quizzes = ShowCatalog();
			if (quizzes.Count == 0)
			{
				return;
			}

			var quizId = ConsoleHelper.ReadInt("Quiz id to start (0 = back)", 0, int.MaxValue);
			if (quizId == 0)
			{
				return;
			}
			var quiz = quizzes.FirstOrDefault(q => q.Id == quizId);
			if (quiz == null)
			{
				ConsoleHelper.Error("There is no published quiz with that id.");
				return;
			}
			if (!ConsoleHelper.Confirm($"Start \"{quiz.Title}\" ({quiz.QuestionCount} questions, {quiz.TimeLimitMinutes} min, pass mark {quiz.PassPercent}%)"))
			{
				return;
			}

			var session = _services.Get<IAttemptService, AttemptSessionDTO>(s => s.StartAttempt(_user.Id, quizId));
			if (session.Resumed)
			{
				ConsoleHelper.Warning($"You already had an unfinished attempt, so it continues ({session.AnsweredCount} of {session.TotalQuestions} answered).");
			}
			ConsoleHelper.Info($"Time runs out at {session.Deadline:HH:mm:ss}. Type q at any question to quit.");

			var number = session.AnsweredCount;
			var timeUp = false;
			foreach (var question in session.Questions)
			{
				number++;
				var outcome = AskQuestion(session, question, number);
				if (outcome == AnswerOutcome.Cancelled)
				{
					return;
				}
				if (outcome == AnswerOutcome.TimeUp)
				{
					timeUp = true;
					break;
				}
			}

			if (timeUp)
			{
				ConsoleHelper.Warning("Time is up! The quiz is finished with the answers you gave.");
			}
			var result = _services.Get<IAttemptService, AttemptResultDTO>(s => s.FinishAttempt(_user.Id, session.AttemptId));
			ResultView.Show(result, justFinished: true);
		}

		private AnswerOutcome AskQuestion(AttemptSessionDTO session, QuestionDTO question, int number)
		{
			while (true)
			{
				var timeLeft = session.Deadline - DateTime.Now;
				if (timeLeft <= TimeSpan.Zero)
				{
					return AnswerOutcome.TimeUp;
				}

				Console.WriteLine();
				ConsoleHelper.WriteColored($"Question {number} of {session.TotalQuestions}   (time left {ConsoleHelper.FormatDuration(timeLeft)})", ConsoleColor.Cyan);
				Console.WriteLine(question.DisplayText);

				var isMulti = question.Type == QuestionType.Multi;
				var input = ConsoleHelper.ReadLine(isMulti ? "Your answers, e.g. 1,3 (q = quit)" : "Your answer (q = quit)");

				if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
				{
					if (ConsoleHelper.Confirm("Quit now? Your attempt will be cancelled"))
					{
						_services.Do<IAttemptService>(s => s.CancelAttempt(_user.Id, session.AttemptId));
						ConsoleHelper.Warning("The attempt was cancelled.");
						return AnswerOutcome.Cancelled;
					}
					continue;
				}

				var numbers = ConsoleHelper.ParseNumbers(input, 1, question.Options.Count);
				if (numbers == null)
				{
					ConsoleHelper.Warning($"Enter {(isMulti ? "one or more numbers" : "a number")} from 1 to {question.Options.Count}.");
					continue;
				}
				if (!isMulti && numbers.Distinct().Count() != 1)
				{
					ConsoleHelper.Warning("Choose exactly one answer.");
					continue;
				}
				if (numbers.Count != numbers.Distinct().Count())
				{
					ConsoleHelper.Info("Repeated choices count once.");
				}

				// Displayed numbers are mapped back to the options' original positions before sending.
				var answer = new SubmitAnswerDTO
				{
					AttemptId = session.AttemptId,
					QuestionId = question.Id,
					SelectedOptionPositions = numbers.Select(n => question.Options[n - 1].Position).ToList()
				};

				try
				{
					_services.Do<IAttemptService>(s => s.SubmitAnswer(_user.Id, answer));
					return AnswerOutcome.Answered;
				}
				catch (TimeLimitExceededException)
				{
					return AnswerOutcome.TimeUp;
				}
				catch (DomainValidationException ex)
				{
					foreach (var error in ex.Errors)
					{
						ConsoleHelper.Warning(error);
					}
				}
			}
		}

		private void ShowMyAttempts()
		{
			var attempts = _services.Get<IAttemptService, List<AttemptDTO>>(s => s.GetStudentAttempts(_user.Id));
			ConsoleHelper.Header("My attempts");
			ConsoleHelper.PrintTable(
				new[] { "Id", "Quiz", "Started", "Finished", "Status", "Score", "Percent", "Result" },
				attempts.Select(a =>
				{
					var finished = a.Status == AttemptStatus.Finished;
					return new[]
					{
						a.Id.ToString(), a.QuizTitle, ConsoleHelper.FormatDate(a.StartedAt), ConsoleHelper.FormatDate(a.FinishedAt),
						a.Status.ToString(), finished ? $"{a.Score}/{a.MaxScore}" : "-",
						finished ? ConsoleHelper.FormatPercent(a.Percent) : "-", a.Result == AttemptResult.None ? "-" : a.Result.ToString()
					};
				}));

			if (!attempts.Any(a => a.Status == AttemptStatus.Finished))
			{
				return;
			}
			var attemptId = ConsoleHelper.ReadInt("Attempt id for details (0 = back)", 0, int.MaxValue);
			if (attemptId == 0)
			{
				return;
			}
			var result = _services.Get<IAttemptService, AttemptResultDTO>(s => s.GetAttemptResult(_user.Id, attemptId));
			ResultView.Show(result, justFinished: false);
		}
	}
}
