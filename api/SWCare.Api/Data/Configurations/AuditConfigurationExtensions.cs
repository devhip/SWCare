using SWCare.Api.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SWCare.Api.Data.Configurations;

internal static class AuditConfigurationExtensions
{
    public static void ConfigureAudit<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditable
    {
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);
    }
}
