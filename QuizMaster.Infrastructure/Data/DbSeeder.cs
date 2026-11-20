using QuizMaster.Domain.Factories;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;

namespace QuizMaster.Infrastructure.Data
{
	/// <summary>
	/// Fills an empty database with the sample data from the assignment. Runs at startup only when there are no users.
	/// </summary>
	public static class DbSeeder
	{
		public static void Seed(QuizMasterDbContext context)
		{
			if (context.Users.Any())
			{
				return;
			}

			var lela = CreateUser(new Lecturer(), "lela_lecturer", "lela@quizmaster.ge", "securePass");
			var luka = CreateUser(new Student(), "luka_student", "luka@example.com", "pass123");
			context.Users.AddRange(lela, luka);

			var oop = new Quiz { Title = "C# OOP Basics", Topic = "Programming", TimeLimitMinutes = 20, PassPercent = 70, Lecturer = lela };
			var lukaAnswers = new List<(Question Question, int[] Answer)>();
			foreach (var (question, answer) in OopQuestions())
			{
				oop.AddQuestion(question);
				lukaAnswers.Add((question, answer));
			}
			oop.Publish();

			var sql = new Quiz { Title = "SQL Fundamentals", Topic = "Databases", TimeLimitMinutes = 15, PassPercent = 60, Lecturer = lela };
			foreach (var question in SqlQuestions())
			{
				sql.AddQuestion(question);
			}
			sql.Publish();

			context.Quizzes.AddRange(oop, sql);
			context.SaveChanges();

			// The sample attempt from the assignment: luka_student scores 16 of 20 (80%) and passes.
			var startedAt = new DateTime(2026, 7, 27, 19, 0, 0);
			var attempt = new Attempt(oop, luka);
			attempt.Start(startedAt, lukaAnswers.Select(a => a.Question.Id));
			context.Attempts.Add(attempt);
			context.SaveChanges();

			var minute = 1;
			foreach (var (question, answer) in lukaAnswers)
			{
				attempt.SubmitAnswer(question, answer, startedAt.AddMinutes(minute++));
			}
			attempt.Finish(oop.PassPercent, startedAt.AddMinutes(14));
			context.SaveChanges();
		}

		private static T CreateUser<T>(T user, string username, string email, string password)
			where T : User
		{
			user.Username = username;
			user.Email = email;
			user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
			user.CreatedAt = DateTime.Now;
			return user;
		}

		// Each question comes with luka_student's answer for the sample attempt. He misses only the last one (4 points).
		private static IEnumerable<(Question Question, int[] Answer)> OopQuestions()
		{
			yield return (Single("Which keyword creates inheritance?", 2,
				new[] { "class", "base", "extends", "inherits" }, 1), new[] { 1 });
			yield return (TrueFalse("A private field is accessible everywhere.", 1, false),
				new[] { TrueFalseQuestion.FalseIndex });
			yield return (Multi("Which of these are OOP principles?", 3,
				new[] { "Encapsulation", "Inheritance", "Compilation", "Polymorphism" }, 0, 1, 3), new[] { 0, 1, 3 });
			yield return (Single("Which access modifier makes a member visible only inside its own class?", 2,
				new[] { "public", "private", "internal", "protected" }, 1), new[] { 1 });
			yield return (TrueFalse("An abstract class can be instantiated directly with new.", 1, false),
				new[] { TrueFalseQuestion.FalseIndex });
			yield return (Multi("Which members can a C# interface declare?", 3,
				new[] { "Methods", "Properties", "Events", "Instance fields" }, 0, 1, 2), new[] { 0, 1, 2 });
			yield return (Single("Which keyword lets a derived class replace a virtual method?", 2,
				new[] { "override", "new", "static", "sealed" }, 0), new[] { 0 });
			yield return (TrueFalse("A C# class can inherit from more than one base class.", 2, false),
				new[] { TrueFalseQuestion.FalseIndex });
			yield return (Multi("Which statements about constructors are true?", 4,
				new[] { "They have the same name as the class", "They can be overloaded", "They must return void", "They can call another constructor with this(...)" },
				0, 1, 3), new[] { 0, 1 });
		}

		private static IEnumerable<Question> SqlQuestions()
		{
			yield return Single("Which SQL statement reads data from a table?", 2,
				new[] { "INSERT", "SELECT", "UPDATE", "DELETE" }, 1);
			yield return Multi("Which of these are SQL aggregate functions?", 3,
				new[] { "COUNT", "AVG", "JOIN", "MAX" }, 0, 1, 3);
			yield return TrueFalse("A primary key column can contain duplicate values.", 1, false);
			yield return Single("Which clause filters groups after GROUP BY?", 2,
				new[] { "WHERE", "HAVING", "ORDER BY", "TOP" }, 1);
			yield return TrueFalse("A foreign key links a row to a row in another table.", 1, true);
		}

		private static Question Single(string text, int points, string[] options, int correct)
		{
			return QuestionFactory.Create(QuestionType.Single, text, points, options, new[] { correct });
		}

		private static Question Multi(string text, int points, string[] options, params int[] correct)
		{
			return QuestionFactory.Create(QuestionType.Multi, text, points, options, correct);
		}

		private static Question TrueFalse(string text, int points, bool correct)
		{
			var answer = correct ? TrueFalseQuestion.TrueIndex : TrueFalseQuestion.FalseIndex;
			return QuestionFactory.Create(QuestionType.TrueFalse, text, points, null, new[] { answer });
		}
	}
}
