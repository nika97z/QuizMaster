using QuizMaster.Application.Services.Interfaces;
using QuizMaster.Domain.Models.DTOs.QuestionDTOs;
using QuizMaster.Domain.Models.DTOs.QuizDTOs;
using QuizMaster.Domain.Models.DTOs.UserDTOs;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.UI.Helpers;

namespace QuizMaster.UI.Menus
{
	/// <summary>
	/// Lecturer screen for one quiz: edit details, manage questions, publish/close and delete.
	/// </summary>
	public class QuizEditor
	{
		private readonly ServiceRunner _services;
		private readonly SafeExecutor _safe;
		private readonly UserDTO _user;
		private readonly int _quizId;

		public QuizEditor(ServiceRunner services, SafeExecutor safe, UserDTO user, int quizId)
		{
			_services = services;
			_safe = safe;
			_user = user;
			_quizId = quizId;
		}

		public void Run()
		{
			while (true)
			{
				QuizDetailsDTO? loaded = null;
				var ok = _safe.Execute(() => loaded = _services.Get<IQuizService, QuizDetailsDTO>(s => s.GetQuizDetails(_user.Id, _quizId)), _user.Id);
				if (!ok || loaded == null)
				{
					return;
				}
				var quiz = loaded;

				ConsoleHelper.Header($"Quiz {quiz.Id} \"{quiz.Title}\" [{(quiz.IsPublished ? "Published" : "Draft/closed")}]");
				Console.WriteLine($"Topic: {quiz.Topic} | {quiz.QuestionCount} questions | max score {quiz.MaxScore} | {quiz.TimeLimitMinutes} min | pass {quiz.PassPercent}%");
				Console.WriteLine("1) Show questions");
				Console.WriteLine("2) Edit details");
				Console.WriteLine("3) Add a question");
				Console.WriteLine("4) Edit a question");
				Console.WriteLine("5) Remove a question");
				Console.WriteLine(quiz.IsPublished ? "6) Close (stop new attempts, allow editing)" : "6) Publish");
				Console.WriteLine("7) Delete quiz");
				Console.WriteLine("0) Back");

				switch (ConsoleHelper.ReadInt("Choose", 0, 7))
				{
					case 1:
						ShowQuestions(quiz);
						break;
					case 2:
						_safe.Execute(() => EditDetails(quiz), _user.Id);
						break;
					case 3:
						_safe.Execute(() => AddQuestion(quiz), _user.Id);
						break;
					case 4:
						_safe.Execute(() => EditQuestion(quiz), _user.Id);
						break;
					case 5:
						_safe.Execute(() => RemoveQuestion(quiz), _user.Id);
						break;
					case 6:
						_safe.Execute(() => TogglePublished(quiz), _user.Id);
						break;
					case 7:
						var deleted = false;
						_safe.Execute(() => deleted = DeleteQuiz(quiz), _user.Id);
						if (deleted)
						{
							return;
						}
						break;
					case 0:
						return;
				}
			}
		}

		private static void ShowQuestions(QuizDetailsDTO quiz)
		{
			ConsoleHelper.Header("Questions");
			if (quiz.Questions.Count == 0)
			{
				ConsoleHelper.Info("No questions yet.");
				return;
			}
			var number = 1;
			foreach (var question in quiz.Questions)
			{
				Console.WriteLine();
				ConsoleHelper.WriteColored($"#{number++}  (id {question.Id}, {question.Type})", ConsoleColor.DarkYellow);
				Console.WriteLine(question.DisplayText);
			}
			Console.WriteLine();
			ConsoleHelper.Info("Correct answers are private to each question, so they aren't shown.");
		}

		private void EditDetails(QuizDetailsDTO quiz)
		{
			if (!IsEditable(quiz))
			{
				return;
			}
			ConsoleHelper.Header("Edit details (press Enter to keep a value)");
			var title = ConsoleHelper.ReadOptional($"Title [{quiz.Title}]");
			var topic = ConsoleHelper.ReadOptional($"Topic [{quiz.Topic}]");
			var timeLimit = ConsoleHelper.ReadOptionalInt($"Time limit in minutes [{quiz.TimeLimitMinutes}]", 1, 300);
			var passPercent = ConsoleHelper.ReadOptionalInt($"Pass percent [{quiz.PassPercent}]", 1, 100);

			var dto = new UpdateQuizDTO
			{
				Id = quiz.Id,
				Title = title.Length == 0 ? quiz.Title : title,
				Topic = topic.Length == 0 ? quiz.Topic : topic,
				TimeLimitMinutes = timeLimit ?? quiz.TimeLimitMinutes,
				PassPercent = passPercent ?? quiz.PassPercent
			};
			_services.Do<IQuizService>(s => s.UpdateQuiz(_user.Id, dto));
			ConsoleHelper.Success("The quiz was updated.");
		}

		private void AddQuestion(QuizDetailsDTO quiz)
		{
			if (!IsEditable(quiz))
			{
				return;
			}
			ConsoleHelper.Header("Add a question");
			Console.WriteLine("Type: 1) Single choice  2) Multiple choice  3) True/False");
			var type = ConsoleHelper.ReadInt("Choose", 1, 3) switch
			{
				1 => QuestionType.Single,
				2 => QuestionType.Multi,
				_ => QuestionType.TrueFalse
			};

			var (text, points, options, correct) = ReadQuestionData(type, current: null);
			var dto = new CreateQuestionDTO
			{
				QuizId = quiz.Id,
				Type = type,
				Text = text,
				Points = points,
				Options = options,
				CorrectOptionIndexes = correct
			};
			var question = _services.Get<IQuestionService, QuestionDTO>(s => s.AddQuestion(_user.Id, dto));
			ConsoleHelper.Success($"Question added (id {question.Id}).");
		}

		private void EditQuestion(QuizDetailsDTO quiz)
		{
			if (!IsEditable(quiz))
			{
				return;
			}
			var question = ChooseQuestion(quiz, "edit");
			if (question == null)
			{
				return;
			}

			ConsoleHelper.Header($"Edit question {question.Id} ({question.Type}), press Enter to keep a value");
			ConsoleHelper.Info("The correct answer is private, so enter it again.");
			var (text, points, options, correct) = ReadQuestionData(question.Type, question);
			var dto = new UpdateQuestionDTO
			{
				Id = question.Id,
				Type = question.Type,
				Text = text,
				Points = points,
				Options = options,
				CorrectOptionIndexes = correct
			};
			_services.Get<IQuestionService, QuestionDTO>(s => s.UpdateQuestion(_user.Id, dto));
			ConsoleHelper.Success("The question was updated.");
		}

		private void RemoveQuestion(QuizDetailsDTO quiz)
		{
			if (!IsEditable(quiz))
			{
				return;
			}
			var question = ChooseQuestion(quiz, "remove");
			if (question == null || !ConsoleHelper.Confirm($"Remove \"{ConsoleHelper.Truncate(question.Text, 50)}\""))
			{
				return;
			}
			_services.Do<IQuestionService>(s => s.RemoveQuestion(_user.Id, question.Id));
			ConsoleHelper.Success("The question was removed.");
		}

		private void TogglePublished(QuizDetailsDTO quiz)
		{
			if (quiz.IsPublished)
			{
				_services.Do<IQuizService>(s => s.CloseQuiz(_user.Id, quiz.Id));
				ConsoleHelper.Success("The quiz is closed. Students can't start it, and you can edit it again.");
			}
			else
			{
				_services.Do<IQuizService>(s => s.PublishQuiz(_user.Id, quiz.Id));
				ConsoleHelper.Success("The quiz is published. Students can take it now.");
			}
		}

		private bool DeleteQuiz(QuizDetailsDTO quiz)
		{
			if (!ConsoleHelper.Confirm($"Delete \"{quiz.Title}\" permanently"))
			{
				return false;
			}
			_services.Do<IQuizService>(s => s.DeleteQuiz(_user.Id, quiz.Id));
			ConsoleHelper.Success("The quiz was deleted.");
			return true;
		}

		private static bool IsEditable(QuizDetailsDTO quiz)
		{
			if (quiz.IsPublished)
			{
				ConsoleHelper.Warning("This quiz is published. Close it (option 6) before making changes.");
				return false;
			}
			return true;
		}

		private static QuestionDTO? ChooseQuestion(QuizDetailsDTO quiz, string action)
		{
			if (quiz.Questions.Count == 0)
			{
				ConsoleHelper.Info("This quiz has no questions yet.");
				return null;
			}
			ConsoleHelper.PrintTable(
				new[] { "#", "Id", "Type", "Points", "Text" },
				quiz.Questions.Select((q, i) => new[] { (i + 1).ToString(), q.Id.ToString(), q.Type.ToString(), q.Points.ToString(), q.Text }));
			var number = ConsoleHelper.ReadInt($"Number (#) of the question to {action} (0 = back)", 0, quiz.Questions.Count);
			return number == 0 ? null : quiz.Questions[number - 1];
		}

		private static (string Text, int Points, List<string> Options, List<int> Correct) ReadQuestionData(QuestionType type, QuestionDTO? current)
		{
			string text;
			int points;
			if (current == null)
			{
				text = ConsoleHelper.ReadNonEmpty("Question text", 1000);
				points = ConsoleHelper.ReadInt("Points", 1, 100);
			}
			else
			{
				var newText = ConsoleHelper.ReadOptional($"Question text [{ConsoleHelper.Truncate(current.Text, 50)}]");
				text = newText.Length == 0 ? current.Text : newText;
				points = ConsoleHelper.ReadOptionalInt($"Points [{current.Points}]", 1, 100) ?? current.Points;
			}

			if (type == QuestionType.TrueFalse)
			{
				var isTrue = ConsoleHelper.ReadTrueFalse("Correct answer (t = true, f = false)");
				var index = isTrue ? TrueFalseQuestion.TrueIndex : TrueFalseQuestion.FalseIndex;
				return (text, points, new List<string>(), new List<int> { index });
			}

			List<string> options;
			if (current != null && ConsoleHelper.Confirm("Keep the current options"))
			{
				options = current.Options.OrderBy(o => o.Position).Select(o => o.Text).ToList();
			}
			else
			{
				var count = ConsoleHelper.ReadInt("How many options", 2, Question.MaxOptions);
				options = new List<string>();
				for (var i = 1; i <= count; i++)
				{
					options.Add(ConsoleHelper.ReadNonEmpty($"Option {i}", 300));
				}
			}

			Console.WriteLine("Options:");
			for (var i = 0; i < options.Count; i++)
			{
				Console.WriteLine($"  {i + 1}) {options[i]}");
			}

			List<int> correct;
			if (type == QuestionType.Single)
			{
				correct = new List<int> { ConsoleHelper.ReadInt("Number of the correct option", 1, options.Count) - 1 };
			}
			else
			{
				while (true)
				{
					var numbers = ConsoleHelper.ParseNumbers(ConsoleHelper.ReadLine("Numbers of all correct options, e.g. 1,3"), 1, options.Count);
					if (numbers != null)
					{
						correct = numbers.Distinct().Select(n => n - 1).ToList();
						break;
					}
					ConsoleHelper.Warning($"Enter one or more numbers from 1 to {options.Count}.");
				}
			}
			return (text, points, options, correct);
		}
	}
}
