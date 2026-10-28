using SWCare.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SWCare.Api.Data.Configurations;

public sealed class TicketDeviceModelConfiguration : IEntityTypeConfiguration<TicketDeviceModel>
{
    public void Configure(EntityTypeBuilder<TicketDeviceModel> builder)
    {
        builder.ToTable("TicketDeviceModels");
        builder.HasKey(x => new { x.TicketId, x.DeviceModelId });

        builder.HasOne(x => x.Ticket).WithMany(x => x.DeviceModels).HasForeignKey(x => x.TicketId);
        builder.HasOne(x => x.DeviceModel).WithMany().HasForeignKey(x => x.DeviceModelId);
    }
}

public sealed class TicketWatcherConfiguration : IEntityTypeConfiguration<TicketWatcher>
{
    public void Configure(EntityTypeBuilder<TicketWatcher> builder)
    {
        builder.ToTable("TicketWatchers");
        builder.ConfigureAudit();
        builder.HasKey(x => new { x.TicketId, x.UserId });

        builder.HasOne(x => x.Ticket).WithMany(x => x.Watchers).HasForeignKey(x => x.TicketId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

public sealed class TicketActivityConfiguration : IEntityTypeConfiguration<TicketActivity>
{
    public void Configure(EntityTypeBuilder<TicketActivity> builder)
    {
        builder.ToTable("TicketActivities");
        builder.ConfigureAudit();

        builder.HasOne(x => x.Ticket).WithMany(x => x.Activities).HasForeignKey(x => x.TicketId);
        builder.HasOne(x => x.Actor).WithMany().HasForeignKey(x => x.ActorId);
    }
}

public sealed class TicketScheduleChangeConfiguration : IEntityTypeConfiguration<TicketScheduleChange>
{
    public void Configure(EntityTypeBuilder<TicketScheduleChange> builder)
    {
        builder.ToTable("TicketScheduleChanges");
        builder.ConfigureAudit();

        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();

        builder.HasOne(x => x.Ticket).WithMany(x => x.ScheduleChanges).HasForeignKey(x => x.TicketId);
        builder.HasOne(x => x.ChangedBy).WithMany().HasForeignKey(x => x.ChangedById);
    }
}

public sealed class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("TicketComments");
        builder.ConfigureAudit();

        builder.Property(x => x.Body).IsRequired();

        builder.HasOne(x => x.Ticket).WithMany(x => x.Comments).HasForeignKey(x => x.TicketId);
        builder.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId);
    }
}

public sealed class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.ToTable("TicketAttachments");
        builder.ConfigureAudit();

        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StoredName).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.StoredName).IsUnique();

        builder.HasOne(x => x.Ticket).WithMany(x => x.Attachments).HasForeignKey(x => x.TicketId);
        builder.HasOne(x => x.Comment).WithMany(x => x.Attachments).HasForeignKey(x => x.CommentId);
        builder.HasOne(x => x.UploadedBy).WithMany().HasForeignKey(x => x.UploadedById);
    }
}

public sealed class TicketLinkConfiguration : IEntityTypeConfiguration<TicketLink>
{
    public void Configure(EntityTypeBuilder<TicketLink> builder)
    {
        // One row per pair: the smaller id is always TicketId.
        builder.ToTable("TicketLinks", table => table.HasCheckConstraint("CK_TicketLinks_Order", "[TicketId] < [LinkedTicketId]"));
        builder.ConfigureAudit();

        builder.HasIndex(x => new { x.TicketId, x.LinkedTicketId }).IsUnique();

        builder.HasOne(x => x.Ticket).WithMany().HasForeignKey(x => x.TicketId);
        builder.HasOne(x => x.LinkedTicket).WithMany().HasForeignKey(x => x.LinkedTicketId);
    }
}

public sealed class TicketSequenceConfiguration : IEntityTypeConfiguration<TicketSequence>
{
    public void Configure(EntityTypeBuilder<TicketSequence> builder)
    {
        builder.ToTable("TicketSequences");
        builder.HasKey(x => x.Year);
        builder.Property(x => x.Year).ValueGeneratedNever();
    }
}
