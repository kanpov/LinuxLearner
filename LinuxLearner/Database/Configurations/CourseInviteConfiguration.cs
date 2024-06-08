using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinuxLearner.Database.Configurations;

public class CourseInviteConfiguration : IEntityTypeConfiguration<CourseInvite>
{
    public void Configure(EntityTypeBuilder<CourseInvite> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.UsageLimit).IsRequired();

        builder.Property(i => i.CourseId).IsRequired();

        builder.ToTable("CourseInvites");
    }
}