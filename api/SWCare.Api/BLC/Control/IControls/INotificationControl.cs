using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;

namespace SWCare.Api.BLC.Control.IControls;

/// <summary>The bell (plan 7): the caller's own notifications only.</summary>
public interface INotificationControl
{
    Task<PagedRes<NotificationRes>> ListAsync(NotificationQueryReq request, CancellationToken cancellationToken);
    Task<int> UnreadCountAsync(CancellationToken cancellationToken);
    Task MarkReadAsync(long id, CancellationToken cancellationToken);
    Task MarkAllReadAsync(CancellationToken cancellationToken);
}
