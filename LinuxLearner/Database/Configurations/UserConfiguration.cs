using LinuxLearner.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinuxLearner.Database.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Name);

        builder.Property(u => u.UserType).IsRequired();

        builder.Property(u => u.RegistrationTime).IsRequired();

        builder.Property(u => u.Description).HasMaxLength(200);

        builder.ToTable("Users");
    }
}