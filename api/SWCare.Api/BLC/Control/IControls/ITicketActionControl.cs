using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;

namespace SWCare.Api.BLC.Control.IControls;

/// <summary>
/// The status changes of plan 4.3. Each one checks TicketWorkflow first (403 ACTION_NOT_ALLOWED), writes the timeline
/// and answers the ticket as the caller now sees it.
/// </summary>
public interface ITicketActionControl
{
    Task<TicketDetailRes> ApproveAsync(int id, CancellationToken cancellationToken);
    Task<TicketDetailRes> RejectAsync(int id, ReasonReq request, CancellationToken cancellationToken);
    Task<TicketDetailRes> ResubmitAsync(int id, CancellationToken cancellationToken);
    Task<TicketDetailRes> CancelAsync(int id, ReasonReq request, CancellationToken cancellationToken);

    /// <summary>The lead sets the real priority, assignee and dates, and may correct the classification (Q21, Q50).</summary>
    Task<TicketDetailRes> TriageAsync(int id, TriageReq request, CancellationToken cancellationToken);

    /// <summary>To WaitingInfo; the reporter's next comment sends it back (Q32).</summary>
    Task<TicketDetailRes> RequestInfoAsync(int id, ReasonReq request, CancellationToken cancellationToken);

    Task<TicketDetailRes> DeclineAsync(int id, ReasonReq request, CancellationToken cancellationToken);
    Task<TicketDetailRes> StartAsync(int id, CancellationToken cancellationToken);
    Task<TicketDetailRes> PauseAsync(int id, CancellationToken cancellationToken);
    Task<TicketDetailRes> ResolveAsync(int id, ResolveReq request, CancellationToken cancellationToken);
    Task<TicketDetailRes> CloseAsync(int id, CancellationToken cancellationToken);
    Task<TicketDetailRes> SendBackAsync(int id, ReasonReq request, CancellationToken cancellationToken);
    Task<TicketDetailRes> RescheduleAsync(int id, RescheduleReq request, CancellationToken cancellationToken);
    Task<TicketDetailRes> ReassignAsync(int id, ReassignReq request, CancellationToken cancellationToken);

    Task<TicketDetailRes> AddWatcherAsync(int id, WatcherReq request, CancellationToken cancellationToken);
    Task<TicketDetailRes> RemoveWatcherAsync(int id, int userId, CancellationToken cancellationToken);
}
