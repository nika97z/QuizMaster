using Microsoft.EntityFrameworkCore;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Infrastructure.Data;

namespace QuizMaster.Infrastructure.Repositories
{
	public class AttemptRepository : IAttemptRepository
	{
		private readonly QuizMasterDbContext _context;

		public AttemptRepository(QuizMasterDbContext context)
		{
			_context = context;
		}

		public void AddAttempt(Attempt attempt)
		{
			_context.Attempts.Add(attempt);
		}

		public void UpdateAttempt(Attempt attempt)
		{
			if (_context.Entry(attempt).State == EntityState.Detached)
			{
				_context.Attempts.Update(attempt);
			}
		}

		public Attempt? GetAttemptById(int id)
		{
			return AttemptsWithDetails().FirstOrDefault(a => a.Id == id);
		}

		public Attempt? GetActiveAttempt(int studentId, int quizId)
		{
			return AttemptsWithDetails()
				.FirstOrDefault(a => a.StudentId == studentId
					&& a.QuizId == quizId
					&& a.Status == AttemptStatus.InProgress);
		}

		public List<Attempt> GetAttemptsByStudent(int studentId)
		{
			return _context.Attempts
				.Include(a => a.Quiz)
				.Where(a => a.StudentId == studentId)
				.OrderByDescending(a => a.StartedAt)
				.ToList();
		}

		public List<Attempt> GetFinishedAttempts()
		{
			return _context.Attempts
				.Include(a => a.Quiz)
				.Include(a => a.Student)
				.Where(a => a.Status == AttemptStatus.Finished)
				.ToList();
		}

		private IQueryable<Attempt> AttemptsWithDetails()
		{
			return _context.Attempts
				.Include(a => a.Answers)
				.Include(a => a.Student)
				.Include(a => a.Quiz)
					.ThenInclude(q => q.Questions)
						.ThenInclude(question => question.Options);
		}
	}
}
