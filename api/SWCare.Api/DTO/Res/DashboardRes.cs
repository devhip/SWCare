using SWCare.Api.Models;

namespace SWCare.Api.DTO.Res;

/// <summary>
/// Dashboard (plan 9, D11). "Now" numbers are the current state; "InRange" numbers count what happened between
/// From and To (Thai dates, inclusive).
/// </summary>
public sealed record DashboardRes(
    DateOnly From,
    DateOnly To,
    bool AllDepartments,
    int? DepartmentId,
    DashboardKpiRes Kpi,
    IReadOnlyList<StatusCountRes> OpenByStatus,
    IReadOnlyList<MonthCountRes> Months,
    IReadOnlyList<BreakdownRes> ByProduct,
    IReadOnlyList<BreakdownRes> ByType,
    IReadOnlyList<BreakdownRes> ByDepartment,
    IReadOnlyList<BreakdownRes> ByCustomer,
    IReadOnlyList<TeamMemberRes> Team,
    IReadOnlyList<StageRes> Stages,
    IReadOnlyList<DeclineGroupRes> Declines);

/// <param name="OnTimePercent">Finished on or before the planned due date, of those finished in the range that had one.</param>
/// <param name="AvgDaysToClose">Report → closed, for tickets closed in the range (flagged imports left out).</param>
public sealed record DashboardKpiRes(
    int OpenNow,
    int OverdueNow,
    int WaitingApprovalNow,
    int WaitingTriageNow,
    int NewInRange,
    int ClosedInRange,
    int DeclinedInRange,
    int CancelledInRange,
    int OnTimeCount,
    int OnTimeBase,
    double? OnTimePercent,
    double? AvgDaysToClose);

public sealed record StatusCountRes(TicketStatus Status, int Count);

/// <param name="Month">yyyy-MM (Thai calendar month, Gregorian year).</param>
public sealed record MonthCountRes(string Month, int New, int Closed);

/// <param name="Key">Id or enum name the web uses to translate or link; Label is the name to show.</param>
/// <param name="Open">How many of them are still open now.</param>
public sealed record BreakdownRes(string Key, string Label, int Count, int Open);

public sealed record TeamMemberRes(
    int UserId,
    string Name,
    int OpenNow,
    int InProgressNow,
    int OverdueNow,
    int ResolvedInRange,
    double? AvgWorkDays,
    int Reschedules);

/// <param name="Stage">Approval, Triage, Queue, Work or Close (plan 9: time spent in each step).</param>
public sealed record StageRes(string Stage, double? AvgDays, int Count);

public sealed record DeclineGroupRes(string Department, int Count, IReadOnlyList<DeclineReasonRes> Recent);

public sealed record DeclineReasonRes(int TicketId, string TicketNo, string Title, string? Reason, DateTime DeclinedAt);
