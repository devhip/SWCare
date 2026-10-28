using System.Text.Json;
using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.Data;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Models;
using SWCare.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.BLC.Control.Controls;

public sealed class TicketActionControl(
    SWCareDbContext db,
    CurrentUser currentUser,
    ITicketControl tickets,
    TicketNotifier notifier,
    TimeProvider clock) : ITicketActionControl
{
    private UserContext Me => currentUser.User;

    public Task<TicketDetailRes> ApproveAsync(int id, CancellationToken cancellationToken) =>
        RunAsync(id, TicketAction.Approve, async (ticket, now) =>
        {
            ticket.ApproverId = Me.Id;
            ticket.ApprovedAt = now;
            Transition(ticket, TicketStatus.PendingTriage, TicketActivityType.Approved);
            // The lead sees "ทำทันที" requests at once (Q22).
            await NotifyAsync(ticket, NotificationType.TicketApproved, cancellationToken, new { RequestedPriority = ticket.RequestedPriority.ToString() });
        }, cancellationToken);

    public Task<TicketDetailRes> RejectAsync(int id, ReasonReq request, CancellationToken cancellationToken) =>
        Simple(id, TicketAction.Reject, TicketStatus.Rejected, TicketActivityType.Rejected, NotificationType.TicketRejected, request.Reason, cancellationToken);

    public Task<TicketDetailRes> ResubmitAsync(int id, CancellationToken cancellationToken) =>
        Simple(id, TicketAction.Resubmit, TicketStatus.PendingApproval, TicketActivityType.Resubmitted, NotificationType.TicketSubmitted, note: null, cancellationToken);

    public Task<TicketDetailRes> CancelAsync(int id, ReasonReq request, CancellationToken cancellationToken) =>
        RunAsync(id, TicketAction.Cancel, async (ticket, now) =>
        {
            var reason = request.Reason.Trim();
            Finish(ticket, now, TicketStatus.Cancelled, TicketActivityType.Cancelled, reason);
            await NotifyAsync(ticket, NotificationType.TicketCancelled, cancellationToken, new { Reason = reason });
        }, cancellationToken);

    public Task<TicketDetailRes> TriageAsync(int id, TriageReq request, CancellationToken cancellationToken) =>
        RunAsync(id, TicketAction.Triage, async (ticket, now) =>
        {
            await EnsureProgrammerAsync(request.AssigneeId, cancellationToken);
            await ReclassifyAsync(ticket, request, cancellationToken);

            ticket.ActualPriority = request.ActualPriority;
            ticket.AssigneeId = request.AssigneeId;
            ticket.PlannedStartDate = request.PlannedStartDate;
            ticket.PlannedDueDate = request.PlannedDueDate;
            ticket.TriagedById = Me.Id;
            ticket.TriagedAt = now;
            ticket.QueueOrder = await NextQueueOrderAsync(request.AssigneeId, cancellationToken);

            var assigneeName = await NameOfAsync(request.AssigneeId, cancellationToken);
            Transition(ticket, TicketStatus.Queued, TicketActivityType.Triaged, data: Json(new
            {
                ActualPriority = request.ActualPriority.ToString(),
                Assignee = assigneeName,
                request.PlannedStartDate,
                request.PlannedDueDate
            }));
            await NotifyAsync(ticket, NotificationType.TicketTriaged, cancellationToken, new
            {
                Assignee = assigneeName,
                PlannedDueDate = request.PlannedDueDate.ToString("yyyy-MM-dd")
            });
        }, cancellationToken);

    public Task<TicketDetailRes> RequestInfoAsync(int id, ReasonReq request, CancellationToken cancellationToken) =>
        RunAsync(id, TicketAction.RequestInfo, async (ticket, _) =>
        {
            var reason = request.Reason.Trim();
            ticket.StatusBeforeWaiting = ticket.Status;
            Transition(ticket, TicketStatus.WaitingInfo, TicketActivityType.InfoRequested, reason);
            await NotifyAsync(ticket, NotificationType.InfoRequested, cancellationToken, new { Reason = reason });
        }, cancellationToken);

    public Task<TicketDetailRes> DeclineAsync(int id, ReasonReq request, CancellationToken cancellationToken) =>
        RunAsync(id, TicketAction.Decline, async (ticket, now) =>
        {
            var reason = request.Reason.Trim();
            ticket.StatusBeforeWaiting = null;
            Finish(ticket, now, TicketStatus.Declined, TicketActivityType.Declined, reason);
            await NotifyAsync(ticket, NotificationType.TicketDeclined, cancellationToken, new { Reason = reason });
        }, cancellationToken);

    public Task<TicketDetailRes> StartAsync(int id, CancellationToken cancellationToken) =>
        RunAsync(id, TicketAction.Start, async (ticket, now) =>
        {
            // The first start ends the reporter's right to cancel (Q40).
            ticket.StartedAt ??= now;
            Transition(ticket, TicketStatus.InProgress, TicketActivityType.Started);
            await NotifyAsync(ticket, NotificationType.TicketStarted, cancellationToken);
        }, cancellationToken);

    public Task<TicketDetailRes> PauseAsync(int id, CancellationToken cancellationToken) =>
        Simple(id, TicketAction.Pause, TicketStatus.Queued, TicketActivityType.Paused, NotificationType.TicketPaused, note: null, cancellationToken);

    public Task<TicketDetailRes> ResolveAsync(int id, ResolveReq request, CancellationToken cancellationToken) =>
        RunAsync(id, TicketAction.Resolve, async (ticket, now) =>
        {
            ticket.Resolution = request.Resolution.Trim();
            ticket.ResolvedAt = now;
            Transition(ticket, TicketStatus.Resolved, TicketActivityType.Resolved);
            await NotifyAsync(ticket, NotificationType.TicketResolved, cancellationToken);
        }, cancellationToken);

    public Task<TicketDetailRes> CloseAsync(int id, CancellationToken cancellationToken) =>
        RunAsync(id, TicketAction.Close, async (ticket, now) =>
        {
            Finish(ticket, now, TicketStatus.Closed, TicketActivityType.Closed, note: null);
            await NotifyAsync(ticket, NotificationType.TicketClosed, cancellationToken);
        }, cancellationToken);

    public Task<TicketDetailRes> SendBackAsync(int id, ReasonReq request, CancellationToken cancellationToken) =>
        RunAsync(id, TicketAction.SendBack, async (ticket, _) =>
        {
            var reason = request.Reason.Trim();
            ticket.ReturnCount++;
            Transition(ticket, TicketStatus.InProgress, TicketActivityType.SentBack, reason);
            await NotifyAsync(ticket, NotificationType.TicketSentBack, cancellationToken, new { Reason = reason });
        }, cancellationToken);

    public Task<TicketDetailRes> RescheduleAsync(int id, RescheduleReq request, CancellationToken cancellationToken) =>
        RunAsync(id, TicketAction.Reschedule, async (ticket, _) =>
        {
            if (ticket.PlannedStartDate == request.PlannedStartDate && ticket.PlannedDueDate == request.PlannedDueDate)
            {
                return;
            }

            var reason = request.Reason.Trim();
            db.TicketScheduleChanges.Add(new TicketScheduleChange
            {
                TicketId = ticket.Id,
                OldStartDate = ticket.PlannedStartDate,
                OldDueDate = ticket.PlannedDueDate,
                NewStartDate = request.PlannedStartDate,
                NewDueDate = request.PlannedDueDate,
                Reason = reason,
                ChangedById = Me.Id
            });
            ticket.Activities.Add(new TicketActivity
            {
                Type = TicketActivityType.Rescheduled,
                ActorId = Me.Id,
                Note = reason,
                Data = Json(new
                {
                    OldStartDate = ticket.PlannedStartDate,
                    OldDueDate = ticket.PlannedDueDate,
                    NewStartDate = request.PlannedStartDate,
                    NewDueDate = request.PlannedDueDate
                })
            });
            ticket.PlannedStartDate = request.PlannedStartDate;
            ticket.PlannedDueDate = request.PlannedDueDate;
            ticket.RescheduleCount++;
            await NotifyAsync(ticket, NotificationType.TicketRescheduled, cancellationToken, new
            {
                NewDueDate = request.PlannedDueDate.ToString("yyyy-MM-dd"),
                Reason = reason
            });
        }, cancellationToken);

    public Task<TicketDetailRes> ReassignAsync(int id, ReassignReq request, CancellationToken cancellationToken) =>
        RunAsync(id, TicketAction.Reassign, async (ticket, _) =>
        {
            if (ticket.AssigneeId == request.AssigneeId)
            {
                return;
            }

            await EnsureProgrammerAsync(request.AssigneeId, cancellationToken);
            var oldId = ticket.AssigneeId;
            var oldName = oldId is { } previous ? await NameOfAsync(previous, cancellationToken) : null;
            var newName = await NameOfAsync(request.AssigneeId, cancellationToken);

            ticket.AssigneeId = request.AssigneeId;
            ticket.QueueOrder = await NextQueueOrderAsync(request.AssigneeId, cancellationToken);
            ticket.Activities.Add(new TicketActivity
            {
                Type = TicketActivityType.Reassigned,
                ActorId = Me.Id,
                Data = Json(new { Old = oldName, New = newName })
            });
            await NotifyAsync(ticket, NotificationType.TicketReassigned, cancellationToken, new { Assignee = newName },
                also: oldId is { } old ? [old] : null);
        }, cancellationToken);

    public Task<TicketDetailRes> AddWatcherAsync(int id, WatcherReq request, CancellationToken cancellationToken) =>
        RunAsync(id, TicketAction.ManageWatchers, async (ticket, _) =>
        {
            if (await db.TicketWatchers.AnyAsync(w => w.TicketId == id && w.UserId == request.UserId, cancellationToken))
            {
                return;
            }

            var name = await db.Users.Where(u => u.Id == request.UserId && u.IsActive).Select(u => u.DisplayName).SingleOrDefaultAsync(cancellationToken)
                       ?? throw new BusinessRuleException("The user is unknown or disabled.", ErrorCodes.InactiveUser);

            db.TicketWatchers.Add(new TicketWatcher { TicketId = id, UserId = request.UserId });
            ticket.Activities.Add(new TicketActivity { Type = TicketActivityType.WatcherAdded, ActorId = Me.Id, Data = Json(new { User = name }) });
            await NotifyAsync(ticket, NotificationType.WatcherAdded, cancellationToken, also: [request.UserId]);
        }, cancellationToken);

    public Task<TicketDetailRes> RemoveWatcherAsync(int id, int userId, CancellationToken cancellationToken) =>
        RunAsync(id, TicketAction.ManageWatchers, async (ticket, _) =>
        {
            var watcher = await db.TicketWatchers.Include(w => w.User)
                .SingleOrDefaultAsync(w => w.TicketId == id && w.UserId == userId, cancellationToken);
            if (watcher is null)
            {
                return;
            }

            db.TicketWatchers.Remove(watcher);
            ticket.Activities.Add(new TicketActivity { Type = TicketActivityType.WatcherRemoved, ActorId = Me.Id, Data = Json(new { User = watcher.User.DisplayName }) });
        }, cancellationToken);

    /// <summary>Load the ticket the caller can see, check the action, apply, save, answer the new state.</summary>
    private async Task<TicketDetailRes> RunAsync(
        int id,
        TicketAction action,
        Func<Ticket, DateTime, Task> apply,
        CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.VisibleTo(Me)
                         .Include(t => t.Product)
                         .Include(t => t.DeviceModels).ThenInclude(m => m.DeviceModel)
                         .SingleOrDefaultAsync(t => t.Id == id, cancellationToken)
                     ?? throw new NotFoundException("Ticket not found.");

        TicketWorkflow.Ensure(TicketWorkflow.StateOf(ticket), Me, action);
        await apply(ticket, clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
        return await tickets.GetAsync(id, cancellationToken);
    }

    private Task<TicketDetailRes> Simple(
        int id,
        TicketAction action,
        TicketStatus to,
        TicketActivityType activity,
        NotificationType notification,
        string? note,
        CancellationToken cancellationToken) =>
        RunAsync(id, action, async (ticket, _) =>
        {
            var text = note?.Trim();
            Transition(ticket, to, activity, text);
            await NotifyAsync(ticket, notification, cancellationToken, text is null ? null : new { Reason = text });
        }, cancellationToken);

    private Task NotifyAsync(Ticket ticket, NotificationType type, CancellationToken cancellationToken, object? extra = null, IEnumerable<int>? also = null) =>
        notifier.NotifyAsync(ticket, type, Me.Id, cancellationToken, extra, also);

    private void Transition(Ticket ticket, TicketStatus to, TicketActivityType activity, string? note = null, string? data = null)
    {
        ticket.Activities.Add(new TicketActivity
        {
            Type = activity,
            FromStatus = ticket.Status,
            ToStatus = to,
            ActorId = Me.Id,
            Note = note,
            Data = data
        });
        ticket.Status = to;
    }

    /// <summary>Closed, Cancelled and Declined: the ticket is over.</summary>
    private void Finish(Ticket ticket, DateTime now, TicketStatus to, TicketActivityType activity, string? note)
    {
        ticket.ClosedAt = now;
        ticket.ClosedById = Me.Id;
        Transition(ticket, to, activity, note);
    }

    /// <summary>Q50: product, type, device models and customer; written to the timeline when anything changes.</summary>
    private async Task ReclassifyAsync(Ticket ticket, TriageReq request, CancellationToken cancellationToken)
    {
        var changes = new List<object>();

        if (ticket.ProductId != request.ProductId)
        {
            var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
                          ?? throw new NotFoundException("System / program not found.");
            if (!product.IsActive)
            {
                throw new BusinessRuleException("This system / program is switched off.", ErrorCodes.InactiveProduct);
            }

            changes.Add(new { Field = "Product", Old = ticket.Product.Name, New = product.Name });
            ticket.ProductId = request.ProductId;
        }

        if (ticket.Type != request.Type)
        {
            changes.Add(new { Field = "Type", Old = ticket.Type.ToString(), New = request.Type.ToString() });
            ticket.Type = request.Type;
        }

        var customer = string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim();
        if (!string.Equals(ticket.CustomerName ?? "", customer ?? "", StringComparison.Ordinal))
        {
            changes.Add(new { Field = "CustomerName", Old = ticket.CustomerName, New = customer });
            ticket.CustomerName = customer;
        }

        var newIds = request.DeviceModelIds.Distinct().ToHashSet();
        var oldIds = ticket.DeviceModels.Select(m => m.DeviceModelId).ToHashSet();
        if (!newIds.SetEquals(oldIds))
        {
            var models = await db.DeviceModels.Where(m => newIds.Contains(m.Id)).ToListAsync(cancellationToken);
            if (models.Count != newIds.Count || models.Any(m => !m.IsActive && !oldIds.Contains(m.Id)))
            {
                throw new BusinessRuleException("A device model is unknown or switched off.", ErrorCodes.InactiveDeviceModel);
            }

            changes.Add(new
            {
                Field = "DeviceModels",
                Old = string.Join(", ", ticket.DeviceModels.Select(m => m.DeviceModel.Name).Order()),
                New = string.Join(", ", models.Select(m => m.Name).Order())
            });

            foreach (var removed in ticket.DeviceModels.Where(m => !newIds.Contains(m.DeviceModelId)).ToList())
            {
                ticket.DeviceModels.Remove(removed);
            }

            foreach (var added in newIds.Except(oldIds))
            {
                ticket.DeviceModels.Add(new TicketDeviceModel { DeviceModelId = added });
            }
        }

        if (changes.Count > 0)
        {
            ticket.Activities.Add(new TicketActivity
            {
                Type = TicketActivityType.ClassificationChanged,
                ActorId = Me.Id,
                Data = Json(new { Changes = changes })
            });
        }
    }

    private async Task EnsureProgrammerAsync(int userId, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId && u.IsActive && u.Department.Kind == DepartmentKind.Software, cancellationToken))
        {
            throw new BusinessRuleException("The assignee must be an active programmer.", ErrorCodes.AssigneeNotProgrammer);
        }
    }

    /// <summary>New work goes to the end of the programmer's queue; the lead reorders it on the Kanban (Q30).</summary>
    private async Task<int> NextQueueOrderAsync(int assigneeId, CancellationToken cancellationToken) =>
        (await db.Tickets
            .Where(t => t.AssigneeId == assigneeId && (t.Status == TicketStatus.Queued || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.WaitingInfo))
            .MaxAsync(t => (int?)t.QueueOrder, cancellationToken) ?? 0) + 1;

    private Task<string> NameOfAsync(int userId, CancellationToken cancellationToken) =>
        db.Users.Where(u => u.Id == userId).Select(u => u.DisplayName).SingleAsync(cancellationToken);

    private static string Json(object value) => JsonSerializer.Serialize(value);
}
