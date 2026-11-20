using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Infrastructure.Data.Configurations
{
	public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
	{
		public void Configure(EntityTypeBuilder<Quiz> builder)
		{
			builder.ToTable("Quizzes", t =>
			{
				t.HasCheckConstraint("CK_Quizzes_TimeLimitMinutes", "[TimeLimitMinutes] BETWEEN 1 AND 300");
				t.HasCheckConstraint("CK_Quizzes_PassPercent", "[PassPercent] BETWEEN 1 AND 100");
			});
			builder.HasKey(q => q.Id);

			builder.Property(q => q.Title).IsRequired().HasMaxLength(200);
			builder.Property(q => q.Topic).IsRequired().HasMaxLength(100);
			builder.Ignore(q => q.QuestionCount);
			builder.Ignore(q => q.MaxScore);

			builder.HasOne(q => q.Lecturer)
				.WithMany(l => l.Quizzes)
				.HasForeignKey(q => q.LecturerId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasMany(q => q.Questions)
				.WithOne(question => question.Quiz)
				.HasForeignKey(question => question.QuizId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasMany(q => q.Attempts)
				.WithOne(a => a.Quiz)
				.HasForeignKey(a => a.QuizId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
