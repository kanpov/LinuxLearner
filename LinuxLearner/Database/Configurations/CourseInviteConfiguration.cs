using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinuxLearner.Database.Configurations;

public class CourseInviteConfiguration : IEntityTypeConfiguration<CourseInvite>
{
    public void Configure(EntityTypeBuilder<CourseInvite> builder)
    {
        builder.HasKey(i => new { i.Id, i.CourseId });

        builder.Property(i => i.Id).ValueGeneratedOnAdd();
        builder.Property(i => i.UsageLimit).IsRequired();
        builder.Property(i => i.UsageAmount).IsRequired();
        builder.Property(i => i.CourseId).IsRequired();

        builder.ToTable("CourseInvites");
    }
}