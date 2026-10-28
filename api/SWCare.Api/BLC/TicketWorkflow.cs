using SWCare.Api.Models;
using SWCare.Api.Services;

namespace SWCare.Api.BLC;

/// <summary>What a user can do to a ticket (plan 4.3). The web shows a button for each allowed one.</summary>
public enum TicketAction
{
    /// <summary>Change the details; only before approval (Q33).</summary>
    Edit,

    /// <summary>Send a rejected ticket back to the department head.</summary>
    Resubmit,

    Approve,
    Reject,

    /// <summary>The reporter, as long as nobody has started on it (Q40).</summary>
    Cancel,

    /// <summary>The programmer lead sets the real priority, assignee and dates (Q21).</summary>
    Triage,

    RequestInfo,

    /// <summary>ไม่ดำเนินการ (Q31).</summary>
    Decline,

    Start,

    /// <summary>Back to the queue to work on something else.</summary>
    Pause,

    /// <summary>The programmer has finished; requires การแก้ไข.</summary>
    Resolve,

    Close,

    /// <summary>The reporter is not satisfied with the fix (Q16).</summary>
    SendBack,

    Reschedule,
    Reassign,
    Comment,

    /// <summary>Attach files to the ticket itself (comments carry their own files).</summary>
    AddAttachment
}

/// <summary>The fields of a ticket the rules look at.</summary>
public sealed record TicketState(
    TicketStatus Status,
    int ReporterId,
    int DepartmentId,
    int? AssigneeId,
    DateTime? StartedAt);

/// <summary>
/// Every status rule in one place (plan 4.3), so the API and the buttons on the web can never disagree.
/// Pure: no database, easy to test.
/// </summary>
public static class TicketWorkflow
{
    public static bool IsFinal(TicketStatus status) =>
        status is TicketStatus.Closed or TicketStatus.Cancelled or TicketStatus.Declined;

    public static TicketState StateOf(Ticket ticket) =>
        new(ticket.Status, ticket.ReporterId, ticket.DepartmentId, ticket.AssigneeId, ticket.StartedAt);

    public static IReadOnlySet<TicketAction> AllowedActions(TicketState ticket, UserContext user)
    {
        // Everyone who can see a ticket can comment on it (Q47).
        var allowed = new HashSet<TicketAction> { TicketAction.Comment };
        if (IsFinal(ticket.Status))
        {
            return allowed;
        }

        var isReporter = ticket.ReporterId == user.Id;
        var isAssignee = ticket.AssigneeId is { } assigneeId && assigneeId == user.Id;
        var isLead = user.IsProgrammerLead;

        // Approver: the head of the department the ticket was reported from (Q2), never someone acting for them (Q29).
        var isApprover = user.IsDepartmentHead && user.DepartmentId == ticket.DepartmentId;

        // Closes and sends back: the reporter, or the reporter's department head (Q57).
        var isCloser = isReporter || isApprover;

        // Once a programmer has started, only the lead can stop it, as Declined (Q40).
        var reporterCanCancel = isReporter && ticket.StartedAt is null;

        // Planned dates exist once the lead has triaged it.
        var isTriaged = ticket.AssigneeId is not null;

        switch (ticket.Status)
        {
            case TicketStatus.PendingApproval:
                AddIf(allowed, isReporter, TicketAction.Edit, TicketAction.AddAttachment, TicketAction.Cancel);
                AddIf(allowed, isApprover, TicketAction.Approve, TicketAction.Reject);
                break;

            case TicketStatus.Rejected:
                AddIf(allowed, isReporter, TicketAction.Edit, TicketAction.AddAttachment, TicketAction.Resubmit, TicketAction.Cancel);
                break;

            case TicketStatus.PendingTriage:
                AddIf(allowed, isLead, TicketAction.Triage, TicketAction.RequestInfo, TicketAction.Decline);
                AddIf(allowed, reporterCanCancel, TicketAction.Cancel);
                break;

            case TicketStatus.WaitingInfo:
                AddIf(allowed, isLead, TicketAction.Decline);
                AddIf(allowed, isTriaged && (isLead || isAssignee), TicketAction.Reschedule);
                AddIf(allowed, isTriaged && isLead, TicketAction.Reassign);
                AddIf(allowed, reporterCanCancel, TicketAction.Cancel);
                break;

            case TicketStatus.Queued:
                AddIf(allowed, isLead || isAssignee, TicketAction.Start, TicketAction.Reschedule);
                AddIf(allowed, isLead, TicketAction.Reassign, TicketAction.Decline);
                AddIf(allowed, reporterCanCancel, TicketAction.Cancel);
                break;

            case TicketStatus.InProgress:
                AddIf(allowed, isLead || isAssignee, TicketAction.Pause, TicketAction.Reschedule);
                AddIf(allowed, isAssignee, TicketAction.Resolve, TicketAction.RequestInfo);
                AddIf(allowed, isLead, TicketAction.Reassign, TicketAction.Decline);
                break;

            case TicketStatus.Resolved:
                AddIf(allowed, isCloser, TicketAction.Close, TicketAction.SendBack);
                break;
        }

        return allowed;
    }

    /// <summary>403 ACTION_NOT_ALLOWED when the user may not do this now.</summary>
    public static void Ensure(TicketState ticket, UserContext user, TicketAction action)
    {
        if (!AllowedActions(ticket, user).Contains(action))
        {
            throw new ForbiddenException($"{action} is not possible for you in this status.", ErrorCodes.ActionNotAllowed);
        }
    }

    private static void AddIf(HashSet<TicketAction> allowed, bool condition, params TicketAction[] actions)
    {
        if (!condition)
        {
            return;
        }

        foreach (var action in actions)
        {
            allowed.Add(action);
        }
    }
}
