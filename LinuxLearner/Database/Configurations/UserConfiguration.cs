using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinuxLearner.Database.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserType).IsRequired();

        builder.HasMany(u => u.Courses)
            .WithMany(c => c.Users)
            .UsingEntity<CourseParticipation>(cu =>
            {
                cu.Property(j => j.IsCourseAdministrator).IsRequired();
                cu.Property(j => j.JoinTime).IsRequired();

                cu.ToTable("CourseParticipations");
            });

        builder.ToTable("Users");
    }
}