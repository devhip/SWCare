using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.Data;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.BLC.Control.Controls;

public sealed class NotificationControl(SWCareDbContext db, CurrentUser currentUser, TimeProvider clock) : INotificationControl
{
    public async Task<PagedRes<NotificationRes>> ListAsync(NotificationQueryReq request, CancellationToken cancellationToken)
    {
        var query = db.Notifications.AsNoTracking().Where(n => n.UserId == currentUser.User.Id);
        if (request.UnreadOnly)
        {
            query = query.Where(n => n.ReadAt == null);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.Id)
            .Skip(request.Skip)
            .Take(request.PageLimit)
            .Select(n => new NotificationRes(
                n.Id,
                n.Type,
                n.TicketId,
                n.Actor == null ? null : n.Actor.DisplayName,
                n.Data,
                n.CreatedAt,
                n.ReadAt != null))
            .ToListAsync(cancellationToken);
        return new PagedRes<NotificationRes>(items, total);
    }

    public Task<int> UnreadCountAsync(CancellationToken cancellationToken) =>
        db.Notifications.CountAsync(n => n.UserId == currentUser.User.Id && n.ReadAt == null, cancellationToken);

    public Task MarkReadAsync(long id, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return db.Notifications
            .Where(n => n.Id == id && n.UserId == currentUser.User.Id && n.ReadAt == null)
            .ExecuteUpdateAsync(setter => setter.SetProperty(n => n.ReadAt, now), cancellationToken);
    }

    public Task MarkAllReadAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return db.Notifications
            .Where(n => n.UserId == currentUser.User.Id && n.ReadAt == null)
            .ExecuteUpdateAsync(setter => setter.SetProperty(n => n.ReadAt, now), cancellationToken);
    }
}
