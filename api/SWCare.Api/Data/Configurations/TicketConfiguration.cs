using SWCare.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SWCare.Api.Data.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.ConfigureAudit();

        builder.Property(x => x.TicketNo).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LegacyRef).HasMaxLength(50);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(50);
        builder.Property(x => x.CustomerName).HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.LegacyReporterName).HasMaxLength(100);
        builder.Property(x => x.LegacyRequesterName).HasMaxLength(100);
        builder.Property(x => x.LegacyApproverName).HasMaxLength(100);
        builder.Property(x => x.LegacyAssigneeName).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.TicketNo).IsUnique();
        builder.HasIndex(x => x.LegacyRef);
        builder.HasIndex(x => x.Status);
        // Kanban: a programmer's cards by status.
        builder.HasIndex(x => new { x.AssigneeId, x.Status });

        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
        builder.HasOne(x => x.Reporter).WithMany().HasForeignKey(x => x.ReporterId);
        builder.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequesterId);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
        builder.HasOne(x => x.Approver).WithMany().HasForeignKey(x => x.ApproverId);
        builder.HasOne(x => x.TriagedBy).WithMany().HasForeignKey(x => x.TriagedById);
        builder.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId);
        builder.HasOne(x => x.ClosedBy).WithMany().HasForeignKey(x => x.ClosedById);
    }
}
