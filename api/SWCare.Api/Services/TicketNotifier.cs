using System.Text.Json;
using SWCare.Api.BLC;
using SWCare.Api.Data;
using SWCare.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.Services;

/// <summary>
/// Adds Notification rows to the context; the caller's SaveChanges stores them with the change itself.
/// The text is built by the reader's client from Type + Data, in their own language (Q49).
/// </summary>
public sealed class TicketNotifier(SWCareDbContext db)
{
    public async Task NotifyAsync(
        Ticket ticket,
        NotificationType type,
        int? actorId,
        CancellationToken cancellationToken,
        object? extra = null,
        IEnumerable<int>? also = null)
    {
        var people = await PeopleOfAsync(ticket, cancellationToken);
        var recipients = NotificationRules.Recipients(type, people, actorId, also);
        if (recipients.Count == 0)
        {
            return;
        }

        var active = await db.Users.Where(u => recipients.Contains(u.Id) && u.IsActive).Select(u => u.Id).ToListAsync(cancellationToken);
        var data = Data(ticket, extra);
        foreach (var userId in active)
        {
            db.Notifications.Add(new Notification { UserId = userId, Type = type, Ticket = ticket, ActorId = actorId, Data = data });
        }
    }

    public static string Data(Ticket ticket, object? extra)
    {
        var values = new Dictionary<string, object?> { ["TicketNo"] = ticket.TicketNo, ["Title"] = ticket.Title };
        if (extra is not null)
        {
            foreach (var property in extra.GetType().GetProperties())
            {
                values[property.Name] = property.GetValue(extra);
            }
        }

        return JsonSerializer.Serialize(values);
    }

    private async Task<TicketPeople> PeopleOfAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        var heads = await db.Departments.AsNoTracking()
            .Where(d => d.HeadUserId != null && (d.Id == ticket.DepartmentId || d.Kind != DepartmentKind.General))
            .Select(d => new { d.Id, d.Kind, d.HeadUserId })
            .ToListAsync(cancellationToken);

        var watchers = ticket.Id == 0
            ? []
            : await db.TicketWatchers.Where(w => w.TicketId == ticket.Id).Select(w => w.UserId).ToListAsync(cancellationToken);

        return new TicketPeople(
            ticket.ReporterId,
            ticket.RequesterId,
            watchers,
            ticket.AssigneeId,
            heads.FirstOrDefault(h => h.Id == ticket.DepartmentId)?.HeadUserId,
            heads.FirstOrDefault(h => h.Kind == DepartmentKind.Support)?.HeadUserId,
            heads.FirstOrDefault(h => h.Kind == DepartmentKind.Software)?.HeadUserId,
            ticket.ApprovedAt is not null);
    }
}
