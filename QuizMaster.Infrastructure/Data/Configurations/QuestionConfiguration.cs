using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Infrastructure.Data.Configurations
{
	public class QuestionConfiguration : IEntityTypeConfiguration<Question>
	{
		public void Configure(EntityTypeBuilder<Question> builder)
		{
			builder.ToTable("Questions", t => t.HasCheckConstraint("CK_Questions_Points", "[Points] > 0"));
			builder.HasKey(q => q.Id);

			builder.Property(q => q.Text).IsRequired().HasMaxLength(1000);

			// TPH: all three question types share the Questions table. The Type column tells them apart.
			builder.Ignore(q => q.Type);
			builder.HasDiscriminator<string>("QuestionKind")
				.HasValue<SingleChoiceQuestion>("Single")
				.HasValue<MultiChoiceQuestion>("Multi")
				.HasValue<TrueFalseQuestion>("TrueFalse");
			builder.Property<string>("QuestionKind").HasColumnName("Type").HasMaxLength(20);

			builder.HasMany(q => q.Options)
				.WithOne()
				.HasForeignKey(o => o.QuestionId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}

	// The correct answers are private fields. EF Core maps them by field name, so no public getter is needed.
	public class SingleChoiceQuestionConfiguration : IEntityTypeConfiguration<SingleChoiceQuestion>
	{
		public void Configure(EntityTypeBuilder<SingleChoiceQuestion> builder)
		{
			builder.Property<int>("_correctOptionIndex").HasColumnName("CorrectOptionIndex");
		}
	}

	public class MultiChoiceQuestionConfiguration : IEntityTypeConfiguration<MultiChoiceQuestion>
	{
		public void Configure(EntityTypeBuilder<MultiChoiceQuestion> builder)
		{
			builder.Property<string>("_correctOptionIndexes").HasColumnName("CorrectOptionIndexes").HasMaxLength(100);
		}
	}

	public class TrueFalseQuestionConfiguration : IEntityTypeConfiguration<TrueFalseQuestion>
	{
		public void Configure(EntityTypeBuilder<TrueFalseQuestion> builder)
		{
			builder.Property<bool>("_correctValue").HasColumnName("CorrectValue");
		}
	}

	public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
	{
		public void Configure(EntityTypeBuilder<QuestionOption> builder)
		{
			builder.ToTable("QuestionOptions");
			builder.HasKey(o => o.Id);
			builder.Property(o => o.Text).IsRequired().HasMaxLength(300);
			builder.HasIndex(o => new { o.QuestionId, o.Position });
		}
	}
}
