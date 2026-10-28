using SWCare.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SWCare.Api.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.ConfigureAudit();

        // Hex SHA-256.
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();

        builder.HasIndex(x => x.TokenHash).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}
