using SWCare.Api.BLC;
using SWCare.Api.Models;

namespace SWCare.Api.Tests.Unit;

/// <summary>Plan 7: who is told about what.</summary>
public sealed class NotificationRulesTests
{
    private const int Reporter = 1, Requester = 2, Watcher = 3, Assignee = 4, DeptHead = 5, SupportHead = 6, Lead = 7;

    private static TicketPeople People(bool approved = true, int? assignee = Assignee) =>
        new(Reporter, Requester, [Watcher], assignee, DeptHead, SupportHead, Lead, approved);

    private static int[] To(NotificationType type, int? actor, TicketPeople? people = null, IEnumerable<int>? also = null) =>
        NotificationRules.Recipients(type, people ?? People(), actor, also).Order().ToArray();

    [Fact]
    public void Submitted_goes_to_the_department_head_and_the_support_head() =>
        Assert.Equal([DeptHead, SupportHead], To(NotificationType.TicketSubmitted, Reporter, People(approved: false, assignee: null)));

    [Fact]
    public void Approved_goes_to_the_lead_the_involved_and_the_support_head() =>
        Assert.Equal([Reporter, Requester, Watcher, SupportHead, Lead], To(NotificationType.TicketApproved, DeptHead, People(assignee: null)));

    [Fact]
    public void Nobody_is_told_about_their_own_action() =>
        Assert.DoesNotContain(Assignee, To(NotificationType.TicketResolved, Assignee));

    [Fact]
    public void Comments_skip_the_support_head_and_the_author() =>
        Assert.Equal([Reporter, Requester, Watcher], To(NotificationType.CommentAdded, Assignee));

    [Fact]
    public void Reschedule_by_the_assignee_also_tells_the_lead()
    {
        Assert.Contains(Lead, To(NotificationType.TicketRescheduled, Assignee));
        Assert.DoesNotContain(Lead, To(NotificationType.TicketRescheduled, Lead));
    }

    [Fact]
    public void Info_provided_before_triage_goes_to_the_lead() =>
        Assert.Equal([SupportHead, Lead], To(NotificationType.InfoProvided, Reporter, People(assignee: null)));

    [Fact]
    public void Cancel_before_approval_does_not_bother_the_programmers()
    {
        Assert.Equal([DeptHead, SupportHead], To(NotificationType.TicketCancelled, Reporter, People(approved: false, assignee: null)));
        Assert.Contains(Lead, To(NotificationType.TicketCancelled, Reporter));
    }

    [Fact]
    public void Reassign_tells_the_old_and_the_new_assignee() =>
        Assert.Equal([Assignee, 9], To(NotificationType.TicketReassigned, Lead, also: [9]));
}
