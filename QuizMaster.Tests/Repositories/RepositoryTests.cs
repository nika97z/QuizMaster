using Microsoft.EntityFrameworkCore;
using QuizMaster.Domain.Factories;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Infrastructure.Data;
using QuizMaster.Infrastructure.Repositories;

namespace QuizMaster.Tests.Repositories
{
	public class RepositoryTests
	{
		private static DbContextOptions<QuizMasterDbContext> NewDatabase()
		{
			return new DbContextOptionsBuilder<QuizMasterDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;
		}

		private static Quiz NewQuiz(Lecturer lecturer, bool publish = true)
		{
			var quiz = new Quiz { Title = "OOP", Topic = "Programming", TimeLimitMinutes = 10, PassPercent = 50, Lecturer = lecturer };
			quiz.AddQuestion(QuestionFactory.Create(QuestionType.Single, "Single?", 2, new[] { "A", "B", "C" }, new[] { 2 }));
			quiz.AddQuestion(QuestionFactory.Create(QuestionType.Multi, "Multi?", 3, new[] { "A", "B", "C" }, new[] { 0, 1 }));
			quiz.AddQuestion(QuestionFactory.Create(QuestionType.TrueFalse, "True?", 1, null, new[] { TrueFalseQuestion.TrueIndex }));
			if (publish)
			{
				quiz.Publish();
			}
			return quiz;
		}

		[Fact]
		public void Questions_KeepTheirTypeAndPrivateCorrectAnswer_AfterReloading()
		{
			var options = NewDatabase();
			int quizId;
			using (var context = new QuizMasterDbContext(options))
			{
				var quiz = NewQuiz(new Lecturer { Username = "lela", Email = "lela@test.ge" });
				new QuizRepository(context).AddQuiz(quiz);
				context.SaveChanges();
				quizId = quiz.Id;
			}

			// A brand-new context, so everything comes from the store, including the private fields.
			using (var context = new QuizMasterDbContext(options))
			{
				var quiz = new QuizRepository(context).GetQuizWithQuestions(quizId);

				Assert.NotNull(quiz);
				Assert.Equal("lela", quiz!.Lecturer.Username);
				var single = Assert.Single(quiz.Questions.OfType<SingleChoiceQuestion>());
				var multi = Assert.Single(quiz.Questions.OfType<MultiChoiceQuestion>());
				var trueFalse = Assert.Single(quiz.Questions.OfType<TrueFalseQuestion>());
				Assert.True(single.CheckAnswer(new[] { 2 }));
				Assert.False(single.CheckAnswer(new[] { 0 }));
				Assert.True(multi.CheckAnswer(new[] { 0, 1 }));
				Assert.True(trueFalse.CheckAnswer(new[] { TrueFalseQuestion.TrueIndex }));
				Assert.Equal(3, single.Options.Count);
				Assert.True(quiz.IsPublished);
				Assert.Equal(6, quiz.MaxScore);
			}
		}

		[Fact]
		public void Attempt_WithAnswers_IsSavedAndLoaded()
		{
			var options = NewDatabase();
			int attemptId;
			using (var context = new QuizMasterDbContext(options))
			{
				var student = new Student { Username = "luka", Email = "luka@test.ge" };
				var quiz = NewQuiz(new Lecturer { Username = "lela", Email = "lela@test.ge" });
				context.Users.Add(student);
				context.Quizzes.Add(quiz);
				context.SaveChanges();

				var attempt = new Attempt(quiz, student);
				attempt.Start(DateTime.Now, quiz.Questions.Select(q => q.Id));
				var repository = new AttemptRepository(context);
				repository.AddAttempt(attempt);
				context.SaveChanges();

				var single = quiz.Questions.OfType<SingleChoiceQuestion>().Single();
				attempt.SubmitAnswer(single, new[] { 2 });
				attempt.Finish(quiz.PassPercent);
				repository.UpdateAttempt(attempt);
				context.SaveChanges();
				attemptId = attempt.Id;
			}

			using (var context = new QuizMasterDbContext(options))
			{
				var attempt = new AttemptRepository(context).GetAttemptById(attemptId);

				Assert.NotNull(attempt);
				Assert.Equal(AttemptStatus.Finished, attempt!.Status);
				Assert.Equal(2, attempt.Score);
				Assert.Equal(6, attempt.MaxScore);
				var answer = Assert.Single(attempt.Answers);
				Assert.True(answer.IsCorrect);
				Assert.Equal("luka", attempt.Student.Username);
				Assert.Equal(3, attempt.Quiz.Questions.Count);
			}
		}

		[Fact]
		public void GetActiveAttempt_ReturnsOnlyTheInProgressAttempt()
		{
			using var context = new QuizMasterDbContext(NewDatabase());
			var student = new Student { Username = "luka", Email = "luka@test.ge" };
			var quiz = NewQuiz(new Lecturer { Username = "lela", Email = "lela@test.ge" });
			context.Users.Add(student);
			context.Quizzes.Add(quiz);
			context.SaveChanges();

			var finished = new Attempt(quiz, student);
			finished.Start(DateTime.Now.AddHours(-2), quiz.Questions.Select(q => q.Id));
			finished.Finish(quiz.PassPercent, DateTime.Now.AddHours(-2).AddMinutes(5));
			var running = new Attempt(quiz, student);
			running.Start(DateTime.Now, quiz.Questions.Select(q => q.Id));
			context.Attempts.AddRange(finished, running);
			context.SaveChanges();

			var active = new AttemptRepository(context).GetActiveAttempt(student.Id, quiz.Id);

			Assert.NotNull(active);
			Assert.Equal(running.Id, active!.Id);
			Assert.True(new QuizRepository(context).HasAttempts(quiz.Id));
		}

		[Fact]
		public void GetUserByUsername_IgnoresCase()
		{
			using var context = new QuizMasterDbContext(NewDatabase());
			var repository = new UserRepository(context);
			repository.AddUser(new Student { Username = "Luka", Email = "Luka@Example.com" });
			context.SaveChanges();

			Assert.NotNull(repository.GetUserByUsername("LUKA"));
			Assert.NotNull(repository.GetUserByEmail("luka@example.com"));
			Assert.IsType<Student>(repository.GetUserByUsername("luka"));
		}

		[Fact]
		public void GetPublishedQuizzes_SkipsDrafts()
		{
			using var context = new QuizMasterDbContext(NewDatabase());
			var lecturer = new Lecturer { Username = "lela", Email = "lela@test.ge" };
			context.Quizzes.AddRange(NewQuiz(lecturer), NewQuiz(lecturer, publish: false));
			context.SaveChanges();

			var published = new QuizRepository(context).GetPublishedQuizzes();

			Assert.Single(published);
			Assert.Equal(2, new QuizRepository(context).GetQuizzesByLecturer(lecturer.Id).Count);
		}
	}
}
