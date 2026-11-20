using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Infrastructure.Data.Configurations
{
	public class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
	{
		public void Configure(EntityTypeBuilder<Attempt> builder)
		{
			builder.ToTable("Attempts", t =>
			{
				t.HasCheckConstraint("CK_Attempts_Score", "[Score] >= 0 AND [Score] <= [MaxScore]");
				t.HasCheckConstraint("CK_Attempts_Percent", "[Percent] BETWEEN 0 AND 100");
			});
			builder.HasKey(a => a.Id);

			builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
			builder.Property(a => a.Result).HasConversion<string>().HasMaxLength(20);
			builder.Property(a => a.Percent).HasPrecision(5, 2);
			builder.Property(a => a.CertificateCode).HasMaxLength(40);
			builder.HasIndex(a => a.CertificateCode).IsUnique().HasFilter("[CertificateCode] IS NOT NULL");
			builder.Property(a => a.QuestionOrder).IsRequired().HasMaxLength(2000);
			builder.Ignore(a => a.IsStarted);
			builder.Ignore(a => a.CorrectCount);
			builder.HasIndex(a => new { a.StudentId, a.QuizId, a.Status });

			builder.HasOne(a => a.Student)
				.WithMany(s => s.Attempts)
				.HasForeignKey(a => a.StudentId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasMany(a => a.Answers)
				.WithOne()
				.HasForeignKey(r => r.AttemptId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}

	public class AnswerRecordConfiguration : IEntityTypeConfiguration<AnswerRecord>
	{
		public void Configure(EntityTypeBuilder<AnswerRecord> builder)
		{
			builder.ToTable("AnswerRecords");
			builder.HasKey(r => r.Id);
			builder.Property(r => r.SelectedOptionIndexes).IsRequired().HasMaxLength(100);
			builder.HasIndex(r => new { r.AttemptId, r.QuestionId }).IsUnique();

			// Restrict: a question that past attempts answered can't be deleted.
			builder.HasOne<Question>()
				.WithMany()
				.HasForeignKey(r => r.QuestionId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}

	public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry>
	{
		public void Configure(EntityTypeBuilder<LogEntry> builder)
		{
			builder.ToTable("LogEntries");
			builder.HasKey(l => l.Id);
			builder.Property(l => l.EventType).HasConversion<string>().HasMaxLength(40);
			builder.Property(l => l.Message).IsRequired().HasMaxLength(2000);
			builder.HasIndex(l => l.Timestamp);
		}
	}
}
