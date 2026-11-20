using Microsoft.EntityFrameworkCore;
using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Infrastructure.Data
{
	public class QuizMasterDbContext : DbContext
	{
		// Used at design time (migrations) when no options are passed in. The app reads appsettings.json instead.
		public const string DefaultConnectionString =
			@"Server=.\SQLEXPRESS;Database=QuizMasterDb;Trusted_Connection=True;TrustServerCertificate=True;";

		public QuizMasterDbContext(DbContextOptions<QuizMasterDbContext> options) : base(options)
		{
		}

		public QuizMasterDbContext()
		{
		}

		public DbSet<User> Users => Set<User>();
		public DbSet<Quiz> Quizzes => Set<Quiz>();
		public DbSet<Question> Questions => Set<Question>();
		public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
		public DbSet<Attempt> Attempts => Set<Attempt>();
		public DbSet<AnswerRecord> AnswerRecords => Set<AnswerRecord>();
		public DbSet<LogEntry> LogEntries => Set<LogEntry>();

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseSqlServer(DefaultConnectionString,
					sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
			}
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuizMasterDbContext).Assembly);
		}
	}
}
