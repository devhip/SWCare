using System.Globalization;
using System.Text.Json;
using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.Data;
using SWCare.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.Services;

public sealed record DailyJobResult(int AutoClosed, int AutoCloseReminders, int ApprovalReminders, int OverdueReminders);

/// <summary>
/// The morning run (plan 7, Q15, Q27): close finished tickets nobody closed, remind the reporter the day before,
/// remind department heads of waiting approvals, tell assignees and the lead about overdue work.
/// </summary>
public sealed class DailyTicketJob(SWCareDbContext db, ISettingControl settings, TicketNotifier notifier, TimeProvider clock)
{
    public async Task<DailyJobResult> RunAsync(CancellationToken cancellationToken)
    {
        var options = await settings.GetAsync(cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(ThaiTime.FromUtc(now));
        var nextWorkingDay = WorkingDays.Add(today, 1);

        // Finished by the programmer, not closed by the reporter.
        var closed = 0;
        var closeReminders = 0;
        var resolved = await db.Tickets.Where(t => t.Status == TicketStatus.Resolved && t.ResolvedAt != null).ToListAsync(cancellationToken);
        foreach (var ticket in resolved)
        {
            var closeDate = WorkingDays.AutoCloseDate(ticket.ResolvedAt!.Value, options.AutoCloseWorkingDays);
            if (today >= closeDate)
            {
                ticket.Activities.Add(new TicketActivity
                {
                    Type = TicketActivityType.AutoClosed,
                    FromStatus = TicketStatus.Resolved,
                    ToStatus = TicketStatus.Closed
                });
                ticket.Status = TicketStatus.Closed;
                ticket.ClosedAt = now;
                ticket.ClosedById = null;
                await notifier.NotifyAsync(ticket, NotificationType.TicketAutoClosed, actorId: null, cancellationToken);
                closed++;
            }
            else if (closeDate <= nextWorkingDay)
            {
                db.Notifications.Add(new Notification
                {
                    UserId = ticket.ReporterId,
                    Type = NotificationType.AutoCloseReminder,
                    Ticket = ticket,
                    Data = TicketNotifier.Data(ticket, new { CloseDate = closeDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) })
                });
                closeReminders++;
            }
        }

        // One reminder per head with the number waiting (Q15).
        var waiting = await db.Tickets
            .Where(t => t.Status == TicketStatus.PendingApproval && t.Department.HeadUserId != null && t.Department.HeadUser!.IsActive)
            .GroupBy(t => t.Department.HeadUserId!.Value)
            .Select(g => new { HeadUserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        foreach (var head in waiting)
        {
            db.Notifications.Add(new Notification
            {
                UserId = head.HeadUserId,
                Type = NotificationType.PendingApprovalReminder,
                Data = JsonSerializer.Serialize(new { head.Count })
            });
        }

        // Late work: the assignee and the lead (Q4).
        var leadId = await db.Departments
            .Where(d => d.Kind == DepartmentKind.Software)
            .Select(d => d.HeadUserId)
            .SingleOrDefaultAsync(cancellationToken);
        var overdue = await db.Tickets
            .Where(t => t.AssigneeId != null
                        && t.PlannedDueDate < today
                        && (t.Status == TicketStatus.Queued || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.WaitingInfo))
            .ToListAsync(cancellationToken);
        var overdueReminders = 0;
        foreach (var ticket in overdue)
        {
            var data = TicketNotifier.Data(ticket, new { PlannedDueDate = ticket.PlannedDueDate!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) });
            foreach (var userId in new[] { ticket.AssigneeId!.Value, leadId ?? 0 }.Where(id => id > 0).Distinct())
            {
                db.Notifications.Add(new Notification { UserId = userId, Type = NotificationType.OverdueReminder, Ticket = ticket, Data = data });
                overdueReminders++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new DailyJobResult(closed, closeReminders, waiting.Count, overdueReminders);
    }
}

/// <summary>Runs DailyTicketJob once per working day, at the reminder time set by the Admin (08:30 Thai time by default).</summary>
public sealed class DailyTicketJobRunner(IServiceScopeFactory scopes, TimeProvider clock, ILogger<DailyTicketJobRunner> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), clock);
        do
        {
            try
            {
                await RunIfDueAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "The morning ticket job failed; it will try again in a minute.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunIfDueAsync(CancellationToken cancellationToken)
    {
        var thaiNow = ThaiTime.Now(clock);
        var today = DateOnly.FromDateTime(thaiNow);
        if (!WorkingDays.IsWorkingDay(today))
        {
            return;
        }

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SWCareDbContext>();
        var options = await scope.ServiceProvider.GetRequiredService<ISettingControl>().GetAsync(cancellationToken);
        if (!TimeOnly.TryParseExact(options.MorningReminderTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var at)
            || TimeOnly.FromDateTime(thaiNow) < at)
        {
            return;
        }

        var todayText = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var lastRun = await db.AppSettings.SingleOrDefaultAsync(s => s.Key == AppSettingKeys.MorningJobLastRun, cancellationToken);
        if (lastRun?.Value == todayText)
        {
            return;
        }

        var result = await scope.ServiceProvider.GetRequiredService<DailyTicketJob>().RunAsync(cancellationToken);

        if (lastRun is null)
        {
            db.AppSettings.Add(new AppSetting { Key = AppSettingKeys.MorningJobLastRun, Value = todayText });
        }
        else
        {
            lastRun.Value = todayText;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Morning ticket job: {Result}", result);
    }
}
