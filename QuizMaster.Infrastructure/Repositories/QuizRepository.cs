using Microsoft.EntityFrameworkCore;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Infrastructure.Data;

namespace QuizMaster.Infrastructure.Repositories
{
	public class QuizRepository : IQuizRepository
	{
		private readonly QuizMasterDbContext _context;

		public QuizRepository(QuizMasterDbContext context)
		{
			_context = context;
		}

		public void AddQuiz(Quiz quiz)
		{
			_context.Quizzes.Add(quiz);
		}

		public void UpdateQuiz(Quiz quiz)
		{
			// Loaded quizzes are already tracked; only detached ones need attaching.
			if (_context.Entry(quiz).State == EntityState.Detached)
			{
				_context.Quizzes.Update(quiz);
			}
		}

		public void DeleteQuiz(Quiz quiz)
		{
			_context.Quizzes.Remove(quiz);
		}

		public Quiz? GetQuizById(int id)
		{
			return _context.Quizzes.Find(id);
		}

		public Quiz? GetQuizWithQuestions(int id)
		{
			return QuizzesWithDetails().FirstOrDefault(q => q.Id == id);
		}

		public List<Quiz> GetPublishedQuizzes()
		{
			return QuizzesWithDetails()
				.Where(q => q.IsPublished)
				.OrderBy(q => q.Topic)
				.ThenBy(q => q.Title)
				.ToList();
		}

		public List<Quiz> GetQuizzesByLecturer(int lecturerId)
		{
			return QuizzesWithDetails()
				.Where(q => q.LecturerId == lecturerId)
				.OrderBy(q => q.Title)
				.ToList();
		}

		public List<Quiz> GetAllQuizzesWithQuestions()
		{
			return QuizzesWithDetails().OrderBy(q => q.Title).ToList();
		}

		public bool HasAttempts(int quizId)
		{
			return _context.Attempts.Any(a => a.QuizId == quizId);
		}

		private IQueryable<Quiz> QuizzesWithDetails()
		{
			return _context.Quizzes
				.Include(q => q.Lecturer)
				.Include(q => q.Questions)
					.ThenInclude(question => question.Options);
		}
	}
}
