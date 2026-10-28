using SWCare.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SWCare.Api.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.ConfigureAudit();

        builder.Property(x => x.Username).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.PasswordHash).HasMaxLength(100);
        builder.Property(x => x.Language).HasMaxLength(5).IsRequired();

        builder.HasIndex(x => x.Username).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

        builder.HasOne(x => x.Department).WithMany(x => x.Members).HasForeignKey(x => x.DepartmentId);
    }
}
