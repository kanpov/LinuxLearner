using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinuxLearner.Database.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired();
        builder.Property(c => c.AcceptanceMode).IsRequired();
        builder.Property(c => c.Discoverable).IsRequired();

        builder.ToTable("Courses");
    }
}