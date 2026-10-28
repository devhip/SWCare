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
}
