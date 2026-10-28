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

    private static TicketState Ticket(TicketStatus status, int? assigneeId = null, bool started = false) =>
        new(status, Reporter.Id, SaleDept, assigneeId, started ? DateTime.UtcNow : null);

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
        Assert.Equal(new[] { TicketAction.Comment }, Allowed(Ticket(TicketStatus.Rejected), SaleHead));
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

        Assert.Equal(new[] { TicketAction.Comment }, Allowed(ticket, OtherProgrammer));
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
    public void Final_statuses_allow_only_comments(TicketStatus status)
    {
        var ticket = Ticket(status, Assignee.Id, started: true);

        foreach (var user in new[] { Reporter, SaleHead, Lead, Assignee, Admin })
        {
            Assert.Equal(new[] { TicketAction.Comment }, Allowed(ticket, user));
        }
    }

    [Fact]
    public void Ensure_throws_action_not_allowed()
    {
        var error = Assert.Throws<ForbiddenException>(() =>
            TicketWorkflow.Ensure(Ticket(TicketStatus.PendingApproval), Reporter, TicketAction.Approve));

        Assert.Equal(ErrorCodes.ActionNotAllowed, error.Code);
    }
}
