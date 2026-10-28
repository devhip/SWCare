using SWCare.Api.DTO.Req;
using SWCare.Api.Models;
using SWCare.Api.Services;

namespace SWCare.Api.BLC;

public static class TicketQueries
{
    /// <summary>
    /// Who sees which tickets (plan 3): Admin, the Support head and programmers see all; everyone else sees what they
    /// reported, requested or watch, and a department head also sees what the department reported (Q26, Q51).
    /// </summary>
    public static IQueryable<Ticket> VisibleTo(this IQueryable<Ticket> tickets, UserContext user)
    {
        if (user.IsAdmin || user.IsSupportHead || user.IsProgrammer)
        {
            return tickets;
        }

        var isHead = user.IsDepartmentHead;
        return tickets.Where(t => t.ReporterId == user.Id
                                  || t.RequesterId == user.Id
                                  || t.Watchers.Any(w => w.UserId == user.Id)
                                  || (isHead && t.DepartmentId == user.DepartmentId));
    }

    /// <summary>The lists of the web. Same rules as TicketWorkflow: e.g. ToApprove = what the user may approve.</summary>
    public static IQueryable<Ticket> InScope(this IQueryable<Ticket> tickets, TicketScope scope, UserContext user)
    {
        var isHead = user.IsDepartmentHead;
        return scope switch
        {
            TicketScope.Reported => tickets.Where(t => t.ReporterId == user.Id),
            TicketScope.Following => tickets.Where(t => t.RequesterId == user.Id || t.Watchers.Any(w => w.UserId == user.Id)),
            TicketScope.ToDo => tickets.Where(t => t.AssigneeId == user.Id
                                                   && (t.Status == TicketStatus.Queued
                                                       || t.Status == TicketStatus.InProgress
                                                       || t.Status == TicketStatus.WaitingInfo)),
            TicketScope.ToApprove => isHead
                ? tickets.Where(t => t.Status == TicketStatus.PendingApproval && t.DepartmentId == user.DepartmentId)
                : tickets.Where(t => false),
            TicketScope.ToClose => tickets.Where(t => t.Status == TicketStatus.Resolved
                                                      && (t.ReporterId == user.Id || (isHead && t.DepartmentId == user.DepartmentId))),
            TicketScope.ToTriage => user.IsProgrammerLead
                ? tickets.Where(t => t.Status == TicketStatus.PendingTriage)
                : tickets.Where(t => false),
            _ => tickets
        };
    }
}
