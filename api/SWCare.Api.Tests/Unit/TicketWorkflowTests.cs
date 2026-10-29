using SWCare.Api.BLC;
using SWCare.Api.Models;
using SWCare.Api.Services;

namespace SWCare.Api.Tests.Unit;

/// <summary>Plan 4.3: who may do what in each status.</summary>
public sealed class TicketWorkflowTests
{
    private const int SaleDept = 2;
    private const int SupportDept = 1;
    private const int SoftwareDept = 4;

    private static readonly UserContext Reporter = User(10, SaleDept, DepartmentKind.General);
    private static readonly UserContext SaleHead = User(11, SaleDept, DepartmentKind.General, isHead: true);
    private static readonly UserContext OtherSales = User(12, SaleDept, DepartmentKind.General);
    private static readonly UserContext SupportHead = User(20, SupportDept, DepartmentKind.Support, isHead: true);
    private static readonly UserContext Lead = User(30, SoftwareDept, DepartmentKind.Software, isHead: true);
    private static readonly UserContext Assignee = User(31, SoftwareDept, DepartmentKind.Software);
    private static readonly UserContext OtherProgrammer = User(32, SoftwareDept, DepartmentKind.Software);
    private static readonly UserContext Admin = User(40, SoftwareDept, DepartmentKind.Software, isAdmin: true);

    private static UserContext User(int id, int departmentId, DepartmentKind kind, bool isHead = false, bool isAdmin = false) =>
        new(id, $"user{id}", $"User {id}", departmentId, "dept", kind, isAdmin, isHead, MustChangePassword: false, "th");

    private static TicketState Ticket(TicketStatus status, int? assigneeId = null, bool started = false, int? requesterId = null, int[]? watcherIds = null) =>
        new(status, Reporter.Id, SaleDept, assigneeId, started ? DateTime.UtcNow : null, requesterId, watcherIds ?? []);

    private static IReadOnlySet<TicketAction> Allowed(TicketState ticket, UserContext user) =>
        TicketWorkflow.AllowedActions(ticket, user);

    [Fact]
    public void Pending_approval_reporter_edits_or_cancels_head_of_reporters_department_decides()
    {
        var ticket = Ticket(TicketStatus.PendingApproval);

        var reporter = Allowed(ticket, Reporter);
        Assert.Contains(TicketAction.Edit, reporter);
        Assert.Contains(TicketAction.AddAttachment, reporter);
        Assert.Contains(TicketAction.Cancel, reporter);
        Assert.DoesNotContain(TicketAction.Approve, Allowed(ticket, Reporter));

        Assert.Contains(TicketAction.Approve, Allowed(ticket, SaleHead));
        Assert.Contains(TicketAction.Reject, Allowed(ticket, SaleHead));
        Assert.DoesNotContain(TicketAction.Edit, Allowed(ticket, SaleHead));
    }

    [Theory]
    [MemberData(nameof(NotTheApprover))]
    public void Only_the_head_of_the_reporters_department_approves(UserContext user)
    {
        var allowed = Allowed(Ticket(TicketStatus.PendingApproval), user);

        Assert.DoesNotContain(TicketAction.Approve, allowed);
        Assert.DoesNotContain(TicketAction.Reject, allowed);
    }

    public static TheoryData<UserContext> NotTheApprover() => [OtherSales, SupportHead, Lead, Admin];

    [Fact]
    public void Rejected_reporter_edits_and_resubmits()
    {
        var allowed = Allowed(Ticket(TicketStatus.Rejected), Reporter);

        Assert.Contains(TicketAction.Edit, allowed);
        Assert.Contains(TicketAction.Resubmit, allowed);
        Assert.Contains(TicketAction.Cancel, allowed);
        Assert.Equal(new[] { TicketAction.Comment, TicketAction.ManageWatchers, TicketAction.Follow }, Allowed(Ticket(TicketStatus.Rejected), SaleHead).Order());
    }

    [Fact]
    public void Pending_triage_only_the_programmer_lead_triages()
    {
        var ticket = Ticket(TicketStatus.PendingTriage);

        var lead = Allowed(ticket, Lead);
        Assert.Contains(TicketAction.Triage, lead);
        Assert.Contains(TicketAction.RequestInfo, lead);
        Assert.Contains(TicketAction.Decline, lead);

        Assert.DoesNotContain(TicketAction.Triage, Allowed(ticket, OtherProgrammer));
        Assert.DoesNotContain(TicketAction.Triage, Allowed(ticket, Admin));
        Assert.DoesNotContain(TicketAction.Edit, Allowed(ticket, Reporter));
        Assert.Contains(TicketAction.Cancel, Allowed(ticket, Reporter));
    }

    [Fact]
    public void Queued_assignee_or_lead_starts_and_reschedules_only_lead_reassigns()
    {
        var ticket = Ticket(TicketStatus.Queued, assigneeId: Assignee.Id);

        var assignee = Allowed(ticket, Assignee);
        Assert.Contains(TicketAction.Start, assignee);
        Assert.Contains(TicketAction.Reschedule, assignee);
        Assert.DoesNotContain(TicketAction.Reassign, assignee);

        var lead = Allowed(ticket, Lead);
        Assert.Contains(TicketAction.Start, lead);
        Assert.Contains(TicketAction.Reassign, lead);
        Assert.Contains(TicketAction.Decline, lead);

        Assert.Equal(new[] { TicketAction.Comment, TicketAction.Follow }, Allowed(ticket, OtherProgrammer).Order());
    }

    [Fact]
    public void In_progress_only_the_assignee_resolves_or_asks_for_information()
    {
        var ticket = Ticket(TicketStatus.InProgress, assigneeId: Assignee.Id, started: true);

        var assignee = Allowed(ticket, Assignee);
        Assert.Contains(TicketAction.Resolve, assignee);
        Assert.Contains(TicketAction.RequestInfo, assignee);
        Assert.Contains(TicketAction.Pause, assignee);

        var lead = Allowed(ticket, Lead);
        Assert.DoesNotContain(TicketAction.Resolve, lead);
        Assert.Contains(TicketAction.Pause, lead);
        Assert.Contains(TicketAction.Decline, lead);
    }

    [Fact]
    public void Reporter_cannot_cancel_once_work_has_started()
    {
        Assert.Contains(TicketAction.Cancel, Allowed(Ticket(TicketStatus.Queued, Assignee.Id), Reporter));
        Assert.DoesNotContain(TicketAction.Cancel, Allowed(Ticket(TicketStatus.Queued, Assignee.Id, started: true), Reporter));
        Assert.DoesNotContain(TicketAction.Cancel, Allowed(Ticket(TicketStatus.InProgress, Assignee.Id, started: true), Reporter));
        Assert.DoesNotContain(TicketAction.Cancel, Allowed(Ticket(TicketStatus.WaitingInfo, Assignee.Id, started: true), Reporter));
    }

    [Fact]
    public void Resolved_reporter_or_their_department_head_closes_or_sends_back()
    {
        var ticket = Ticket(TicketStatus.Resolved, Assignee.Id, started: true);

        foreach (var user in new[] { Reporter, SaleHead })
        {
            Assert.Contains(TicketAction.Close, Allowed(ticket, user));
            Assert.Contains(TicketAction.SendBack, Allowed(ticket, user));
        }

        Assert.DoesNotContain(TicketAction.Close, Allowed(ticket, Assignee));
        Assert.DoesNotContain(TicketAction.Close, Allowed(ticket, SupportHead));
    }

    [Theory]
    [InlineData(TicketStatus.Closed)]
    [InlineData(TicketStatus.Cancelled)]
    [InlineData(TicketStatus.Declined)]
    public void Final_statuses_allow_only_comments_and_watchers(TicketStatus status)
    {
        var ticket = Ticket(status, Assignee.Id, started: true);

        Assert.Equal(new[] { TicketAction.Comment, TicketAction.ManageWatchers }, Allowed(ticket, Reporter).Order());
        Assert.Equal(new[] { TicketAction.Comment, TicketAction.ManageWatchers, TicketAction.Follow }, Allowed(ticket, SaleHead).Order());
        Assert.Equal(new[] { TicketAction.Comment, TicketAction.ManageWatchers, TicketAction.Follow }, Allowed(ticket, Lead).Order());
        foreach (var user in new[] { Assignee, Admin })
        {
            Assert.Equal(new[] { TicketAction.Comment, TicketAction.Follow }, Allowed(ticket, user).Order());
        }
    }

    [Fact]
    public void Reporter_their_head_and_the_lead_manage_watchers()
    {
        // Also once the ticket is over, e.g. a salesperson who wants to see how it was fixed (D14).
        foreach (var status in new[] { TicketStatus.PendingApproval, TicketStatus.Queued, TicketStatus.Resolved, TicketStatus.Closed })
        {
            var ticket = Ticket(status, Assignee.Id);
            Assert.Contains(TicketAction.ManageWatchers, Allowed(ticket, Reporter));
            Assert.Contains(TicketAction.ManageWatchers, Allowed(ticket, SaleHead));
            Assert.Contains(TicketAction.ManageWatchers, Allowed(ticket, Lead));
            Assert.DoesNotContain(TicketAction.ManageWatchers, Allowed(ticket, Assignee));
            Assert.DoesNotContain(TicketAction.ManageWatchers, Allowed(ticket, SupportHead));
        }
    }

    [Fact]
    public void Executive_only_reads_other_peoples_tickets()
    {
        // ผู้บริหาร in the Sale department: not its head, not a programmer (D10)
        var executive = new UserContext(50, "boss", "Boss", SaleDept, "dept", DepartmentKind.General, false, false, MustChangePassword: false, "th", IsExecutive: true);

        foreach (var status in Enum.GetValues<TicketStatus>())
        {
            Assert.Equal(new[] { TicketAction.Follow }, Allowed(Ticket(status, Assignee.Id), executive));
        }

        // Their own ticket works as for any reporter.
        var own = new TicketState(TicketStatus.PendingApproval, executive.Id, SaleDept, null, null);
        Assert.Contains(TicketAction.Comment, Allowed(own, executive));
        Assert.Contains(TicketAction.Cancel, Allowed(own, executive));

        // An executive who is also Admin keeps commenting.
        var adminExecutive = executive with { IsAdmin = true };
        Assert.Contains(TicketAction.Comment, Allowed(Ticket(TicketStatus.Queued, Assignee.Id), adminExecutive));
    }

    [Fact]
    public void View_all_user_reads_until_they_follow()
    {
        // ดูงานทั้งหมด: a salesperson looking up a customer's history (D14)
        var sales = OtherSales with { CanViewAllTickets = true };
        Assert.True(sales.SeesAllTickets);

        foreach (var status in Enum.GetValues<TicketStatus>())
        {
            Assert.Equal(new[] { TicketAction.Follow }, Allowed(Ticket(status, Assignee.Id), sales));

            // Following makes them a watcher: comment and unfollow, nothing more.
            var following = Ticket(status, Assignee.Id, watcherIds: [sales.Id]);
            Assert.Equal(new[] { TicketAction.Comment, TicketAction.Unfollow }, Allowed(following, sales).Order());
        }

        // The switch does not take anything away from a programmer.
        var programmer = OtherProgrammer with { CanViewAllTickets = true };
        Assert.False(programmer.SeesAllReadOnly);
        Assert.Contains(TicketAction.Comment, Allowed(Ticket(TicketStatus.Queued, Assignee.Id), programmer));
    }

    [Fact]
    public void Anyone_who_sees_a_ticket_follows_it_except_who_is_already_told()
    {
        var ticket = Ticket(TicketStatus.Queued, Assignee.Id, requesterId: OtherSales.Id, watcherIds: [SupportHead.Id]);

        Assert.Contains(TicketAction.Follow, Allowed(ticket, Lead));
        Assert.Contains(TicketAction.Follow, Allowed(ticket, Admin));
        Assert.DoesNotContain(TicketAction.Follow, Allowed(ticket, Reporter));
        Assert.DoesNotContain(TicketAction.Follow, Allowed(ticket, OtherSales));

        var watcher = Allowed(ticket, SupportHead);
        Assert.Contains(TicketAction.Unfollow, watcher);
        Assert.DoesNotContain(TicketAction.Follow, watcher);

        // Also after it is closed.
        var closed = ticket with { Status = TicketStatus.Closed };
        Assert.Contains(TicketAction.Follow, Allowed(closed, Lead));
        Assert.Contains(TicketAction.Unfollow, Allowed(closed, SupportHead));
    }

    [Fact]
    public void Ensure_throws_action_not_allowed()
    {
        var error = Assert.Throws<ForbiddenException>(() =>
            TicketWorkflow.Ensure(Ticket(TicketStatus.PendingApproval), Reporter, TicketAction.Approve));

        Assert.Equal(ErrorCodes.ActionNotAllowed, error.Code);
    }
}
