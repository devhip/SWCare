using System.Globalization;
using SWCare.Api.DTO.Res;
using SWCare.Api.Models;
using SWCare.Api.Services;

namespace SWCare.Api.BLC;

/// <summary>One ticket as the Dashboard needs it (already limited to what the user may see).</summary>
public sealed record DashboardTicket(
    int Id,
    string TicketNo,
    string? LegacyRef,
    string Title,
    TicketType Type,
    int ProductId,
    string ProductName,
    string? CustomerName,
    int DepartmentId,
    string DepartmentName,
    TicketStatus Status,
    TicketPriority Priority,
    string ReporterName,
    int? AssigneeId,
    string? AssigneeName,
    DateTime ReportedAt,
    DateTime? ApprovedAt,
    DateTime? TriagedAt,
    DateTime? StartedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    DateOnly? PlannedDueDate,
    int RescheduleCount,
    int ReturnCount,
    bool NeedsLegacyReview,
    bool IsImported);

/// <summary>A date change (เลื่อนวัน) and the ticket's programmer.</summary>
public sealed record DashboardReschedule(int TicketId, int? AssigneeId, DateTime ChangedAt);

/// <summary>The reason the programmer lead wrote when declining (ไม่ดำเนินการ).</summary>
public sealed record DashboardDecline(int TicketId, string? Reason);

/// <summary>A person on the Dashboard team table.</summary>
public sealed record DashboardPerson(int Id, string Name);

/// <summary>Thai-date range [From, To], both days included.</summary>
public sealed record DashboardRange(DateOnly From, DateOnly To)
{
    public DateTime FromUtc => ThaiDayStartUtc(From);
    public DateTime ToUtcExclusive => ThaiDayStartUtc(To.AddDays(1));

    public bool Contains(DateTime? utc) => utc is { } value && value >= FromUtc && value < ToUtcExclusive;

    public static DateTime ThaiDayStartUtc(DateOnly day) => day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);
}

/// <summary>Every Dashboard number (plan 9, D11), computed in memory from the tickets: pure and unit-tested.</summary>
public static class DashboardStats
{
    public static readonly TicketStatus[] OpenStatuses =
    [
        TicketStatus.PendingApproval, TicketStatus.Rejected, TicketStatus.PendingTriage, TicketStatus.WaitingInfo,
        TicketStatus.Queued, TicketStatus.InProgress, TicketStatus.Resolved
    ];

    private static readonly TicketStatus[] WorkingStatuses = [TicketStatus.Queued, TicketStatus.InProgress, TicketStatus.WaitingInfo];

    public static bool IsOpen(TicketStatus status) => OpenStatuses.Contains(status);

    /// <summary>Same rule as IsOverdue of the ticket lists.</summary>
    public static bool IsOverdue(DashboardTicket t, DateOnly today) =>
        WorkingStatuses.Contains(t.Status) && t.PlannedDueDate is { } due && due < today;

    /// <summary>Finished on or before the planned due date (Thai date of ResolvedAt).</summary>
    public static bool IsOnTime(DashboardTicket t) =>
        t.ResolvedAt is { } resolved && t.PlannedDueDate is { } due && DateOnly.FromDateTime(ThaiTime.FromUtc(resolved)) <= due;

    public static DashboardRes Build(
        IReadOnlyList<DashboardTicket> tickets,
        IReadOnlyList<DashboardReschedule> reschedules,
        IReadOnlyList<DashboardDecline> declines,
        IReadOnlyList<DashboardPerson> programmers,
        DashboardRange range,
        DateOnly today,
        bool allDepartments,
        int? departmentId)
    {
        var reported = tickets.Where(t => range.Contains(t.ReportedAt)).ToList();
        var closed = tickets.Where(t => t.Status == TicketStatus.Closed && range.Contains(t.ClosedAt)).ToList();
        var resolvedWithDue = tickets.Where(t => range.Contains(t.ResolvedAt) && t.PlannedDueDate is not null && !t.NeedsLegacyReview).ToList();
        var onTime = resolvedWithDue.Count(IsOnTime);

        var kpi = new DashboardKpiRes(
            OpenNow: tickets.Count(t => IsOpen(t.Status)),
            OverdueNow: tickets.Count(t => IsOverdue(t, today)),
            WaitingApprovalNow: tickets.Count(t => t.Status == TicketStatus.PendingApproval),
            WaitingTriageNow: tickets.Count(t => t.Status == TicketStatus.PendingTriage),
            NewInRange: reported.Count,
            ClosedInRange: closed.Count,
            DeclinedInRange: tickets.Count(t => t.Status == TicketStatus.Declined && range.Contains(t.ClosedAt)),
            CancelledInRange: tickets.Count(t => t.Status == TicketStatus.Cancelled && range.Contains(t.ClosedAt)),
            OnTimeCount: onTime,
            OnTimeBase: resolvedWithDue.Count,
            OnTimePercent: resolvedWithDue.Count == 0 ? null : Math.Round(100.0 * onTime / resolvedWithDue.Count, 1),
            AvgDaysToClose: Average(closed.Where(t => !t.NeedsLegacyReview).Select(t => Days(t.ReportedAt, t.ClosedAt))));

        var openByStatus = OpenStatuses
            .Select(status => new StatusCountRes(status, tickets.Count(t => t.Status == status)))
            .ToList();

        return new DashboardRes(
            range.From,
            range.To,
            allDepartments,
            departmentId,
            kpi,
            openByStatus,
            Months(tickets, range),
            Breakdown(reported, t => t.ProductId.ToString(CultureInfo.InvariantCulture), t => t.ProductName),
            Breakdown(reported, t => t.Type.ToString(), t => t.Type.ToString()),
            Breakdown(reported, t => t.DepartmentId.ToString(CultureInfo.InvariantCulture), t => t.DepartmentName),
            Breakdown(reported.Where(t => !string.IsNullOrWhiteSpace(t.CustomerName)).ToList(),
                t => t.CustomerName!.Trim().ToLowerInvariant(), t => t.CustomerName!.Trim()).Take(10).ToList(),
            Team(tickets, reschedules, programmers, range, today),
            Stages(closed.Where(t => !t.NeedsLegacyReview).ToList()),
            Declined(tickets, declines, range));
    }

    /// <summary>Every month of the range, also months without tickets, so the chart has no gaps.</summary>
    private static List<MonthCountRes> Months(IReadOnlyList<DashboardTicket> tickets, DashboardRange range)
    {
        static string Key(DateTime utc) => ThaiTime.FromUtc(utc).ToString("yyyy-MM", CultureInfo.InvariantCulture);

        var created = tickets.Where(t => range.Contains(t.ReportedAt)).GroupBy(t => Key(t.ReportedAt)).ToDictionary(g => g.Key, g => g.Count());
        var closed = tickets.Where(t => t.Status == TicketStatus.Closed && range.Contains(t.ClosedAt))
            .GroupBy(t => Key(t.ClosedAt!.Value)).ToDictionary(g => g.Key, g => g.Count());

        var months = new List<MonthCountRes>();
        for (var month = new DateOnly(range.From.Year, range.From.Month, 1); month <= range.To; month = month.AddMonths(1))
        {
            var key = month.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            months.Add(new MonthCountRes(key, created.GetValueOrDefault(key), closed.GetValueOrDefault(key)));
        }

        return months;
    }

    private static List<BreakdownRes> Breakdown(
        IReadOnlyList<DashboardTicket> tickets,
        Func<DashboardTicket, string> key,
        Func<DashboardTicket, string> label) =>
        tickets.GroupBy(key)
            .Select(g => new BreakdownRes(g.Key, label(g.First()), g.Count(), g.Count(t => IsOpen(t.Status))))
            .OrderByDescending(b => b.Count)
            .ThenBy(b => b.Label, StringComparer.CurrentCulture)
            .ToList();

    private static List<TeamMemberRes> Team(
        IReadOnlyList<DashboardTicket> tickets,
        IReadOnlyList<DashboardReschedule> reschedules,
        IReadOnlyList<DashboardPerson> programmers,
        DashboardRange range,
        DateOnly today)
    {
        // Programmers of the Software department, plus anyone else who still holds work.
        var people = programmers.ToDictionary(p => p.Id, p => p.Name);
        foreach (var t in tickets.Where(t => t.AssigneeId is not null && t.AssigneeName is not null))
        {
            people.TryAdd(t.AssigneeId!.Value, t.AssigneeName!);
        }

        return people
            .Select(person =>
            {
                var mine = tickets.Where(t => t.AssigneeId == person.Key).ToList();
                var resolved = mine.Where(t => range.Contains(t.ResolvedAt)).ToList();
                return new TeamMemberRes(
                    person.Key,
                    person.Value,
                    OpenNow: mine.Count(t => WorkingStatuses.Contains(t.Status)),
                    InProgressNow: mine.Count(t => t.Status == TicketStatus.InProgress),
                    OverdueNow: mine.Count(t => IsOverdue(t, today)),
                    ResolvedInRange: resolved.Count,
                    AvgWorkDays: Average(resolved.Where(t => !t.NeedsLegacyReview).Select(t => Days(t.StartedAt, t.ResolvedAt))),
                    Reschedules: reschedules.Count(r => r.AssigneeId == person.Key && range.Contains(r.ChangedAt)));
            })
            .Where(m => m.OpenNow + m.ResolvedInRange + m.Reschedules > 0 || programmers.Any(p => p.Id == m.UserId))
            .OrderByDescending(m => m.OpenNow)
            .ThenByDescending(m => m.ResolvedInRange)
            .ThenBy(m => m.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    /// <summary>
    /// Average days in each step, for tickets closed in the range. Imported tickets have no real approval time
    /// (approved = reported in the sheet), so they are left out of that step.
    /// </summary>
    private static List<StageRes> Stages(IReadOnlyList<DashboardTicket> closed)
    {
        StageRes Stage(string name, IEnumerable<double?> values)
        {
            var list = values.Where(v => v is >= 0).Select(v => v!.Value).ToList();
            return new StageRes(name, list.Count == 0 ? null : Math.Round(list.Average(), 1), list.Count);
        }

        return
        [
            Stage("Approval", closed.Where(t => !t.IsImported).Select(t => Days(t.ReportedAt, t.ApprovedAt))),
            Stage("Triage", closed.Select(t => Days(t.ApprovedAt, t.TriagedAt))),
            Stage("Queue", closed.Select(t => Days(t.TriagedAt, t.StartedAt))),
            Stage("Work", closed.Select(t => Days(t.StartedAt, t.ResolvedAt))),
            Stage("Close", closed.Select(t => Days(t.ResolvedAt, t.ClosedAt)))
        ];
    }

    /// <summary>Plan 9: which department sends work that is not a programming problem.</summary>
    private static List<DeclineGroupRes> Declined(IReadOnlyList<DashboardTicket> tickets, IReadOnlyList<DashboardDecline> declines, DashboardRange range)
    {
        var reasons = declines.GroupBy(d => d.TicketId).ToDictionary(g => g.Key, g => g.Last().Reason);
        return tickets
            .Where(t => t.Status == TicketStatus.Declined && range.Contains(t.ClosedAt))
            .GroupBy(t => t.DepartmentName)
            .Select(g => new DeclineGroupRes(
                g.Key,
                g.Count(),
                g.OrderByDescending(t => t.ClosedAt)
                    .Take(5)
                    .Select(t => new DeclineReasonRes(t.Id, t.TicketNo, t.Title, reasons.GetValueOrDefault(t.Id), t.ClosedAt!.Value))
                    .ToList()))
            .OrderByDescending(g => g.Count)
            .ToList();
    }

    public static double? Days(DateTime? from, DateTime? to) =>
        from is { } start && to is { } end ? (end - start).TotalDays : null;

    private static double? Average(IEnumerable<double?> values)
    {
        var list = values.Where(v => v is >= 0).Select(v => v!.Value).ToList();
        return list.Count == 0 ? null : Math.Round(list.Average(), 1);
    }
}
