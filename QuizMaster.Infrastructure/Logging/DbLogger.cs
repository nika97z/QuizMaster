using Microsoft.EntityFrameworkCore;
using QuizMaster.Domain.Interfaces;
using QuizMaster.Domain.Models.Entities;
using QuizMaster.Domain.Models.Enums;
using QuizMaster.Infrastructure.Data;

namespace QuizMaster.Infrastructure.Logging
{
	/// <summary>
	/// Writes log entries to the LogEntries table. Each entry is saved at once in its own short-lived
	/// DbContext, so it is kept even when the business operation fails. Logging never crashes the app.
	/// </summary>
	public class DbLogger : IAppLogger
	{
		private const int MaxMessageLength = 2000;
		private readonly IDbContextFactory<QuizMasterDbContext> _contextFactory;

		public DbLogger(IDbContextFactory<QuizMasterDbContext> contextFactory)
		{
			_contextFactory = contextFactory;
		}

		public void Log(LogEventType eventType, string message, int? userId = null, int? quizId = null, int? attemptId = null)
		{
			try
			{
				using var context = _contextFactory.CreateDbContext();
				context.LogEntries.Add(new LogEntry
				{
					Timestamp = DateTime.Now,
					EventType = eventType,
					Message = Truncate(message),
					UserId = userId,
					QuizId = quizId,
					AttemptId = attemptId
				});
				context.SaveChanges();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"[log not saved] {eventType}: {message} ({ex.GetBaseException().Message})");
			}
		}

		public List<LogEntry> GetRecentEntries(int count)
		{
			try
			{
				using var context = _contextFactory.CreateDbContext();
				return context.LogEntries
					.AsNoTracking()
					.OrderByDescending(l => l.Timestamp)
					.ThenByDescending(l => l.Id)
					.Take(Math.Clamp(count, 1, 500))
					.ToList();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"[log not read] {ex.GetBaseException().Message}");
				return new List<LogEntry>();
			}
		}

		private static string Truncate(string? message)
		{
			var text = message ?? string.Empty;
			return text.Length <= MaxMessageLength ? text : text[..(MaxMessageLength - 3)] + "...";
		}
	}
}
