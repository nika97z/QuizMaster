using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Tests.TestHelpers
{
	/// <summary>
	/// Builds test objects. Every quiz from PublishedQuiz() has three questions:
	/// Single (2 points, correct option 1), Multi (3 points, correct options 0 and 2), True/False (1 point, correct: false).
	/// Max score 6.
	/// </summary>
	internal static class TestData
	{
		public static readonly DateTime Now = new DateTime(2026, 11, 18, 10, 0, 0);

		public static Lecturer Lecturer(int id = 1, string username = "lela")
		{
			return new Lecturer { Id = id, Username = username, Email = $"{username}@test.ge" };
		}

		public static Student Student(int id = 2, string username = "luka")
		{
			return new Student { Id = id, Username = username, Email = $"{username}@test.ge" };
		}

		public static SingleChoiceQuestion Single(int id, int correct = 1, int points = 2)
		{
			return WithId(new SingleChoiceQuestion("Single?", points, new[] { "A", "B", "C" }, correct), id);
		}

		public static MultiChoiceQuestion Multi(int id, int points = 3, params int[] correct)
		{
			var answers = correct.Length == 0 ? new[] { 0, 2 } : correct;
			return WithId(new MultiChoiceQuestion("Multi?", points, new[] { "A", "B", "C", "D" }, answers), id);
		}

		public static TrueFalseQuestion TrueFalse(int id, bool correct = false, int points = 1)
		{
			return WithId(new TrueFalseQuestion("True or false?", points, correct), id);
		}

		public static Quiz PublishedQuiz(int id = 10, Lecturer? lecturer = null, int passPercent = 50, string topic = "Programming")
		{
			lecturer ??= Lecturer();
			var quiz = new Quiz
			{
				Id = id,
				Title = $"Quiz {id}",
				Topic = topic,
				TimeLimitMinutes = 20,
				PassPercent = passPercent,
				LecturerId = lecturer.Id,
				Lecturer = lecturer
			};
			quiz.AddQuestion(Single(id * 10 + 1));
			quiz.AddQuestion(Multi(id * 10 + 2));
			quiz.AddQuestion(TrueFalse(id * 10 + 3));
			quiz.Publish();
			return quiz;
		}

		public static Attempt StartedAttempt(Quiz quiz, Student student, DateTime? startedAt = null, int id = 0)
		{
			var attempt = new Attempt(quiz, student);
			attempt.Start(startedAt ?? Now, quiz.Questions.Select(q => q.Id));
			if (id > 0)
			{
				SetId(attempt, id);
			}
			return attempt;
		}

		/// <summary>
		/// A finished attempt where the first <paramref name="correctAnswers"/> questions are answered correctly
		/// and the rest wrongly. With PublishedQuiz(): 0 → 0, 1 → 2, 2 → 5, 3 → 6 points.
		/// </summary>
		public static Attempt FinishedAttempt(Quiz quiz, Student student, int correctAnswers, DateTime startedAt, int minutes = 5)
		{
			var attempt = StartedAttempt(quiz, student, startedAt);
			var index = 0;
			foreach (var question in quiz.Questions.OrderBy(q => q.OrderIndex))
			{
				var answer = index < correctAnswers ? CorrectAnswer(question) : WrongAnswer(question);
				attempt.SubmitAnswer(question, answer, startedAt.AddMinutes(1));
				index++;
			}
			attempt.Finish(quiz.PassPercent, startedAt.AddMinutes(minutes));
			return attempt;
		}

		public static int[] CorrectAnswer(Question question)
		{
			return question switch
			{
				SingleChoiceQuestion => new[] { 1 },
				MultiChoiceQuestion => new[] { 0, 2 },
				_ => new[] { TrueFalseQuestion.FalseIndex }
			};
		}

		public static int[] WrongAnswer(Question question)
		{
			return question switch
			{
				SingleChoiceQuestion => new[] { 0 },
				MultiChoiceQuestion => new[] { 0 },
				_ => new[] { TrueFalseQuestion.TrueIndex }
			};
		}

		/// <summary>
		/// Simulates the database assigning an id (Attempt.Id has a private setter).
		/// </summary>
		public static void SetId(Attempt attempt, int id)
		{
			typeof(Attempt).GetProperty(nameof(Attempt.Id))!.SetValue(attempt, id);
		}

		private static T WithId<T>(T question, int id) where T : Question
		{
			question.Id = id;
			return question;
		}
	}
}
