using SWCare.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SWCare.Api.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.ConfigureAudit();

        // The bell: a user's newest notifications.
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Ticket).WithMany().HasForeignKey(x => x.TicketId);
        builder.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorId);
    }
}
