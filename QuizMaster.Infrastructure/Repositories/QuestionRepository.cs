using Microsoft.EntityFrameworkCore;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Infrastructure.Data;

namespace QuizMaster.Infrastructure.Repositories
{
	public class QuestionRepository : IQuestionRepository
	{
		private readonly QuizMasterDbContext _context;

		public QuestionRepository(QuizMasterDbContext context)
		{
			_context = context;
		}

		public void AddQuestion(Question question)
		{
			_context.Questions.Add(question);
		}

		public void UpdateQuestion(Question question)
		{
			if (_context.Entry(question).State == EntityState.Detached)
			{
				_context.Questions.Update(question);
			}
		}

		public void DeleteQuestion(Question question)
		{
			_context.Questions.Remove(question);
		}

		public Question? GetQuestionById(int id)
		{
			return _context.Questions
				.Include(q => q.Options)
				.FirstOrDefault(q => q.Id == id);
		}

		public bool HasAnswers(int questionId)
		{
			return _context.AnswerRecords.Any(a => a.QuestionId == questionId);
		}
	}
}
