using System.Globalization;
using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.Data;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Models;
using SWCare.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.BLC.Control.Controls;

/// <summary>
/// Plan 9 / D11: executives, the Support head, the programmer lead and Admin see every department;
/// any other department head sees only their own (Q46).
/// </summary>
public sealed class DashboardControl(SWCareDbContext db, CurrentUser currentUser, TimeProvider clock) : IDashboardControl
{
    public async Task<DashboardRes> GetAsync(DashboardReq request, CancellationToken cancellationToken) =>
        (await LoadAsync(request, cancellationToken)).Result;

    public async Task<(byte[] Content, string FileName)> ExportAsync(DashboardReq request, CancellationToken cancellationToken)
    {
        var data = await LoadAsync(request, cancellationToken);
        var range = new DashboardRange(data.Result.From, data.Result.To);
        var tickets = data.Tickets.Where(t => range.Contains(t.ReportedAt)).OrderBy(t => t.ReportedAt).ToList();
        var departmentName = data.Result.DepartmentId is { } id
            ? await db.Departments.Where(d => d.Id == id).Select(d => d.Name).SingleOrDefaultAsync(cancellationToken)
            : null;
        var productName = request.ProductId is { } productId
            ? await db.Products.Where(p => p.Id == productId).Select(p => p.Name).SingleOrDefaultAsync(cancellationToken)
            : null;

        var content = DashboardExcel.Build(data.Result, tickets, departmentName, productName, currentUser.User.Language, ThaiTime.Now(clock));
        var fileName = string.Create(CultureInfo.InvariantCulture, $"SWCare-Dashboard-{range.From:yyyyMMdd}-{range.To:yyyyMMdd}.xlsx");
        return (content, fileName);
    }

    private async Task<(DashboardRes Result, List<DashboardTicket> Tickets)> LoadAsync(DashboardReq request, CancellationToken cancellationToken)
    {
        var user = currentUser.User;
        var allDepartments = user.IsExecutive || user.IsAdmin || user.IsSupportHead || user.IsProgrammerLead;
        int? departmentId = allDepartments ? request.DepartmentId : user.DepartmentId;

        var today = DateOnly.FromDateTime(ThaiTime.Now(clock));
        var from = request.From ?? new DateOnly(today.Year, today.Month, 1).AddMonths(-5);
        var to = request.To ?? today;
        if (to < from)
        {
            to = from;
        }

        var range = new DashboardRange(from, to);

        var scope = db.Tickets.AsNoTracking();
        if (departmentId is { } department)
        {
            scope = scope.Where(t => t.DepartmentId == department);
        }

        if (request.ProductId is { } product)
        {
            scope = scope.Where(t => t.ProductId == product);
        }

        var tickets = await scope
            .Select(t => new DashboardTicket(
                t.Id,
                t.TicketNo,
                t.LegacyRef,
                t.Title,
                t.Type,
                t.ProductId,
                t.Product.Name,
                t.CustomerName,
                t.DepartmentId,
                t.Department.Name,
                t.Status,
                t.ActualPriority ?? t.RequestedPriority,
                t.LegacyReporterName ?? t.Reporter.DisplayName,
                t.AssigneeId,
                t.Assignee != null ? t.Assignee.DisplayName : t.LegacyAssigneeName,
                t.ReportedAt,
                t.ApprovedAt,
                t.TriagedAt,
                t.StartedAt,
                t.ResolvedAt,
                t.ClosedAt,
                t.PlannedDueDate,
                t.RescheduleCount,
                t.ReturnCount,
                t.NeedsLegacyReview,
                t.Activities.Any(a => a.Type == TicketActivityType.Imported)))
            .ToListAsync(cancellationToken);

        var reschedules = await db.TicketScheduleChanges.AsNoTracking()
            .Where(c => c.CreatedAt >= range.FromUtc && c.CreatedAt < range.ToUtcExclusive)
            .Where(c => scope.Any(t => t.Id == c.TicketId))
            .Select(c => new DashboardReschedule(c.TicketId, c.Ticket.AssigneeId, c.CreatedAt))
            .ToListAsync(cancellationToken);

        var declines = await db.TicketActivities.AsNoTracking()
            .Where(a => a.Type == TicketActivityType.Declined && a.Ticket.Status == TicketStatus.Declined)
            .Where(a => scope.Any(t => t.Id == a.TicketId))
            .OrderBy(a => a.Id)
            .Select(a => new DashboardDecline(a.TicketId, a.Note))
            .ToListAsync(cancellationToken);

        var programmers = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Department.Kind == DepartmentKind.Software)
            .Select(u => new DashboardPerson(u.Id, u.DisplayName))
            .ToListAsync(cancellationToken);

        var result = DashboardStats.Build(tickets, reschedules, declines, programmers, range, today, allDepartments, departmentId);
        return (result, tickets);
    }
}
