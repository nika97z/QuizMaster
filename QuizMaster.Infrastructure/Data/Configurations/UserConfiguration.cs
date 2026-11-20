using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizMaster.Domain.Models.Entities;

namespace QuizMaster.Infrastructure.Data.Configurations
{
	public class UserConfiguration : IEntityTypeConfiguration<User>
	{
		public void Configure(EntityTypeBuilder<User> builder)
		{
			builder.ToTable("Users");
			builder.HasKey(u => u.Id);

			builder.Property(u => u.Username).IsRequired().HasMaxLength(30);
			builder.HasIndex(u => u.Username).IsUnique();
			builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
			builder.HasIndex(u => u.Email).IsUnique();
			builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(128);

			// TPH: Lecturer and Student share the Users table. The Role column tells them apart.
			builder.Ignore(u => u.Role);
			builder.HasDiscriminator<string>("UserType")
				.HasValue<Student>("student")
				.HasValue<Lecturer>("lecturer");
			builder.Property<string>("UserType").HasColumnName("Role").HasMaxLength(20);
		}
	}
}
