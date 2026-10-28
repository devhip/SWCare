using SWCare.Api.Models;

namespace SWCare.Api.BLC;

/// <summary>The people around one ticket, as ids.</summary>
public sealed record TicketPeople(
    int ReporterId,
    int? RequesterId,
    IReadOnlyCollection<int> WatcherIds,
    int? AssigneeId,
    int? DepartmentHeadId,
    int? SupportHeadId,
    int? ProgrammerLeadId,
    bool WasApproved);

/// <summary>Who hears about what (plan 7). Pure, so it can be tested; TicketNotifier writes the rows.</summary>
public static class NotificationRules
{
    public static IReadOnlySet<int> Recipients(NotificationType type, TicketPeople people, int? actorId, IEnumerable<int>? also = null)
    {
        var to = new HashSet<int>();

        // ผู้เกี่ยวข้อง = คนแจ้ง + ผู้ร้องขอ + ผู้ติดตาม.
        void Involved()
        {
            Add(people.ReporterId);
            Add(people.RequesterId);
            foreach (var watcher in people.WatcherIds)
            {
                to.Add(watcher);
            }
        }

        void Add(int? userId)
        {
            if (userId is { } id)
            {
                to.Add(id);
            }
        }

        switch (type)
        {
            case NotificationType.TicketSubmitted:
                Add(people.DepartmentHeadId);
                Add(people.SupportHeadId);
                break;

            case NotificationType.TicketApproved:
                Add(people.ProgrammerLeadId);
                Involved();
                Add(people.SupportHeadId);
                break;

            case NotificationType.TicketRejected:
            case NotificationType.InfoRequested:
                Add(people.ReporterId);
                Add(people.RequesterId);
                Add(people.SupportHeadId);
                break;

            case NotificationType.TicketTriaged:
                Add(people.AssigneeId);
                Involved();
                Add(people.SupportHeadId);
                break;

            case NotificationType.InfoProvided:
                Add(people.AssigneeId ?? people.ProgrammerLeadId);
                Add(people.SupportHeadId);
                break;

            case NotificationType.TicketStarted:
            case NotificationType.TicketPaused:
            case NotificationType.TicketResolved:
            case NotificationType.TicketDeclined:
                Involved();
                Add(people.SupportHeadId);
                break;

            case NotificationType.TicketRescheduled:
                Involved();
                // The lead set the dates; tell them when the programmer moves them (Q23).
                if (actorId is not null && actorId == people.AssigneeId)
                {
                    Add(people.ProgrammerLeadId);
                }

                break;

            case NotificationType.TicketReassigned:
                // The old assignee comes in "also".
                Add(people.AssigneeId);
                break;

            case NotificationType.TicketSentBack:
                Add(people.AssigneeId);
                Add(people.ProgrammerLeadId);
                Add(people.SupportHeadId);
                break;

            case NotificationType.TicketClosed:
            case NotificationType.TicketAutoClosed:
                Add(people.AssigneeId);
                Involved();
                Add(people.SupportHeadId);
                break;

            case NotificationType.TicketCancelled:
                Add(people.DepartmentHeadId);
                if (people.WasApproved)
                {
                    Add(people.AssigneeId);
                    Add(people.ProgrammerLeadId);
                }

                Add(people.SupportHeadId);
                break;

            case NotificationType.CommentAdded:
                // Not the Support head: comments are too many (Q57).
                Involved();
                Add(people.AssigneeId);
                break;

            case NotificationType.WatcherAdded:
                // The new watcher only, in "also".
                break;
        }

        foreach (var userId in also ?? [])
        {
            to.Add(userId);
        }

        // Nobody is told about what they did themselves.
        if (actorId is { } actor)
        {
            to.Remove(actor);
        }

        return to;
    }
}
