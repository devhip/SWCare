using SWCare.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SWCare.Api.Data.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.ConfigureAudit();

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();

        // Only one Support and one Software department: their heads have system-wide roles.
        builder.HasIndex(x => x.Kind).IsUnique().HasFilter("[Kind] <> 'General'");

        builder.HasOne(x => x.HeadUser).WithMany().HasForeignKey(x => x.HeadUserId);
    }
}
