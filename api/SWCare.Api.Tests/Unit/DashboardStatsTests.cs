using SWCare.Api.BLC;
using SWCare.Api.DTO.Res;
using SWCare.Api.Models;

namespace SWCare.Api.Tests.Unit;

/// <summary>Plan 9 / D11: the Dashboard numbers.</summary>
public sealed class DashboardStatsTests
{
    private static readonly DateOnly Today = new(2026, 10, 28);
    private static readonly DashboardRange Range = new(new DateOnly(2026, 9, 1), Today);

    /// <summary>Thai date + hour → UTC.</summary>
    private static DateTime Thai(int month, int day, int hour = 10) => new DateTime(2026, month, day, hour, 0, 0, DateTimeKind.Utc).AddHours(-7);

    private static DashboardTicket T(
        int id,
        TicketStatus status,
        DateTime reported,
        DateTime? approved = null,
        DateTime? triaged = null,
        DateTime? started = null,
        DateTime? resolved = null,
        DateTime? closed = null,
        DateOnly? due = null,
        int? assigneeId = null,
        TicketType type = TicketType.Bug,
        string? customer = null,
        int departmentId = 1,
        bool review = false,
        bool imported = false) =>
        new(id, $"SW-2026-{id:D4}", null, $"Ticket {id}", type, 1, "HIP TIME 4.0", customer, departmentId, departmentId == 1 ? "Customer Support" : "Sale",
            status, TicketPriority.Normal, "Reporter", assigneeId, assigneeId is null ? null : "Dev", reported, approved, triaged, started, resolved, closed, due,
            0, 0, review, imported);

    private static DashboardRes Build(IReadOnlyList<DashboardTicket> tickets, IReadOnlyList<DashboardReschedule>? reschedules = null, IReadOnlyList<DashboardDecline>? declines = null) =>
        DashboardStats.Build(tickets, reschedules ?? [], declines ?? [], [new DashboardPerson(7, "Dev"), new DashboardPerson(8, "Idle dev")], Range, Today, true, null);

    [Fact]
    public void Range_uses_thai_days()
    {
        // 31 Aug 23:30 Thai is outside; 1 Sep 00:30 Thai (31 Aug 17:30 UTC) is inside.
        Assert.False(Range.Contains(Thai(8, 31, 23).AddMinutes(30)));
        Assert.True(Range.Contains(Thai(9, 1, 0).AddMinutes(30)));
        Assert.True(Range.Contains(Thai(10, 28, 23)));
        Assert.False(Range.Contains(Thai(10, 29, 0)));
    }

    [Fact]
    public void Key_numbers()
    {
        var tickets = new[]
        {
            T(1, TicketStatus.Closed, Thai(9, 1), Thai(9, 1), Thai(9, 2), Thai(9, 3), Thai(9, 4), Thai(9, 6), due: new DateOnly(2026, 9, 5), assigneeId: 7),
            T(2, TicketStatus.Closed, Thai(9, 10), Thai(9, 10), Thai(9, 11), Thai(9, 12), Thai(9, 20), Thai(9, 21), due: new DateOnly(2026, 9, 15), assigneeId: 7),
            T(3, TicketStatus.InProgress, Thai(10, 1), Thai(10, 1), Thai(10, 2), Thai(10, 3), due: new DateOnly(2026, 10, 20), assigneeId: 7),
            T(4, TicketStatus.PendingApproval, Thai(10, 27)),
            T(5, TicketStatus.PendingTriage, Thai(8, 15), Thai(8, 15)),
            T(6, TicketStatus.Declined, Thai(10, 5), closed: Thai(10, 6)),
            T(7, TicketStatus.Cancelled, Thai(10, 7), closed: Thai(10, 7))
        };

        var kpi = Build(tickets).Kpi;

        Assert.Equal(3, kpi.OpenNow); // 3, 4, 5
        Assert.Equal(1, kpi.OverdueNow); // 3: due 20 Oct, still in progress
        Assert.Equal(1, kpi.WaitingApprovalNow);
        Assert.Equal(1, kpi.WaitingTriageNow);
        Assert.Equal(6, kpi.NewInRange); // not 5, reported in August
        Assert.Equal(2, kpi.ClosedInRange);
        Assert.Equal(1, kpi.DeclinedInRange);
        Assert.Equal(1, kpi.CancelledInRange);
        Assert.Equal(1, kpi.OnTimeCount); // 1 finished 4 Sep, due 5 Sep; 2 finished 20 Sep, due 15 Sep
        Assert.Equal(2, kpi.OnTimeBase);
        Assert.Equal(50, kpi.OnTimePercent);
        Assert.Equal(8, kpi.AvgDaysToClose); // 5 and 11 days
    }

    [Fact]
    public void Nothing_finished_means_no_percentages()
    {
        var kpi = Build([T(1, TicketStatus.PendingApproval, Thai(10, 1))]).Kpi;

        Assert.Null(kpi.OnTimePercent);
        Assert.Null(kpi.AvgDaysToClose);
    }

    [Fact]
    public void Every_month_of_the_range_is_listed()
    {
        var range = new DashboardRange(new DateOnly(2026, 7, 15), Today);
        var tickets = new[]
        {
            T(1, TicketStatus.Closed, Thai(7, 20), closed: Thai(9, 2)),
            T(2, TicketStatus.Queued, Thai(9, 30, 23)) // still September in Thailand
        };

        var months = DashboardStats.Build(tickets, [], [], [], range, Today, true, null).Months;

        Assert.Equal(["2026-07", "2026-08", "2026-09", "2026-10"], months.Select(m => m.Month));
        Assert.Equal([1, 0, 1, 0], months.Select(m => m.New));
        Assert.Equal([0, 0, 1, 0], months.Select(m => m.Closed));
    }

    [Fact]
    public void Breakdowns_count_the_range_and_what_is_still_open()
    {
        var tickets = new[]
        {
            T(1, TicketStatus.Closed, Thai(9, 1), closed: Thai(9, 2), customer: "The River", type: TicketType.FeatureRequest),
            T(2, TicketStatus.Queued, Thai(9, 3), customer: " the river ", departmentId: 2),
            T(3, TicketStatus.Queued, Thai(9, 4), customer: "Hyde Park"),
            T(4, TicketStatus.Queued, Thai(8, 1), customer: "Hyde Park") // before the range
        };

        var result = Build(tickets);

        var river = result.ByCustomer.First();
        Assert.Equal(("The River", 2, 1), (river.Label, river.Count, river.Open));
        Assert.Equal(2, result.ByCustomer.Count);
        Assert.Equal(["Bug", "FeatureRequest"], result.ByType.Select(b => b.Key));
        Assert.Equal([2, 1], result.ByDepartment.Select(b => b.Count));
    }

    [Fact]
    public void Stages_average_closed_tickets_and_skip_the_approval_of_imports()
    {
        var tickets = new[]
        {
            T(1, TicketStatus.Closed, Thai(9, 1), Thai(9, 2), Thai(9, 3), Thai(9, 5), Thai(9, 9), Thai(9, 10)),
            T(2, TicketStatus.Closed, Thai(9, 1), Thai(9, 1), null, Thai(9, 1), Thai(9, 3), Thai(9, 7), imported: true),
            T(3, TicketStatus.Closed, Thai(9, 1), Thai(9, 1), Thai(9, 1), Thai(9, 1), Thai(9, 2), Thai(9, 3), review: true)
        };

        var stages = Build(tickets).Stages.ToDictionary(s => s.Stage);

        Assert.Equal((1.0, 1), (stages["Approval"].AvgDays, stages["Approval"].Count));
        Assert.Equal((1.0, 1), (stages["Triage"].AvgDays, stages["Triage"].Count));
        Assert.Equal((2.0, 1), (stages["Queue"].AvgDays, stages["Queue"].Count));
        Assert.Equal((3.0, 2), (stages["Work"].AvgDays, stages["Work"].Count));
        Assert.Equal((2.5, 2), (stages["Close"].AvgDays, stages["Close"].Count));
    }

    [Fact]
    public void Team_lists_every_programmer_with_their_work()
    {
        var tickets = new[]
        {
            T(1, TicketStatus.InProgress, Thai(9, 1), started: Thai(9, 2), due: new DateOnly(2026, 10, 1), assigneeId: 7),
            T(2, TicketStatus.Queued, Thai(9, 1), assigneeId: 7),
            T(3, TicketStatus.Closed, Thai(9, 1), started: Thai(9, 2), resolved: Thai(9, 6), closed: Thai(9, 7), assigneeId: 7)
        };
        DashboardReschedule[] reschedules = [new(1, 7, Thai(9, 20)), new(1, 7, Thai(8, 1))];

        var team = Build(tickets, reschedules).Team;

        Assert.Equal(["Dev", "Idle dev"], team.Select(m => m.Name));
        var dev = team[0];
        Assert.Equal((2, 1, 1, 1, 4.0, 1), (dev.OpenNow, dev.InProgressNow, dev.OverdueNow, dev.ResolvedInRange, dev.AvgWorkDays, dev.Reschedules));
    }

    [Fact]
    public void Declines_group_by_department_with_the_latest_reason()
    {
        var tickets = new[]
        {
            T(1, TicketStatus.Declined, Thai(9, 1), closed: Thai(9, 2)),
            T(2, TicketStatus.Declined, Thai(9, 3), closed: Thai(9, 4)),
            T(3, TicketStatus.Declined, Thai(9, 5), closed: Thai(9, 6), departmentId: 2)
        };
        DashboardDecline[] declines = [new(1, "Firmware"), new(2, "first"), new(2, "Fixed on site")];

        var groups = Build(tickets, declines: declines).Declines;

        Assert.Equal(("Customer Support", 2), (groups[0].Department, groups[0].Count));
        Assert.Equal(["Fixed on site", "Firmware"], groups[0].Recent.Select(r => r.Reason));
    }
}
