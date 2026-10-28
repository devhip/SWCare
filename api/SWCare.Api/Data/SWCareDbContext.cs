using SWCare.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SWCare.Api.Data;

public sealed class SWCareDbContext(DbContextOptions<SWCareDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<DeviceModel> DeviceModels => Set<DeviceModel>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketDeviceModel> TicketDeviceModels => Set<TicketDeviceModel>();
    public DbSet<TicketWatcher> TicketWatchers => Set<TicketWatcher>();
    public DbSet<TicketActivity> TicketActivities => Set<TicketActivity>();
    public DbSet<TicketScheduleChange> TicketScheduleChanges => Set<TicketScheduleChange>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketLink> TicketLinks => Set<TicketLink>();
    public DbSet<TicketSequence> TicketSequences => Set<TicketSequence>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SWCareDbContext).Assembly);

        // Rows are never deleted (Q54), and SQL Server rejects the many cascade paths between users and tickets.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(32);

        // All DateTime columns are UTC; mark them as such when reading.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
