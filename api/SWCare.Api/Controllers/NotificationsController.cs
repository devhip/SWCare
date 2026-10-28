using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace SWCare.Api.Controllers;

/// <summary>The bell. Each user sees only their own notifications.</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Notifications")]
public sealed class NotificationsController(INotificationControl control) : ControllerBase
{
    /// <summary>Newest first. Data is JSON with TicketNo, Title and the event's values.</summary>
    [HttpGet]
    public async Task<ResultsViewModel<NotificationRes>> List([FromQuery] NotificationQueryReq request, CancellationToken cancellationToken) =>
        ApiResult.OkPage(await control.ListAsync(request, cancellationToken), request);

    [HttpGet("UnreadCount")]
    public async Task<ResultViewModel<int>> UnreadCount(CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.UnreadCountAsync(cancellationToken));

    [HttpPost("{id:long}/Read")]
    public async Task<ResultViewModel<bool>> Read(long id, CancellationToken cancellationToken)
    {
        await control.MarkReadAsync(id, cancellationToken);
        return ApiResult.Ok(true);
    }

    [HttpPost("ReadAll")]
    public async Task<ResultViewModel<bool>> ReadAll(CancellationToken cancellationToken)
    {
        await control.MarkAllReadAsync(cancellationToken);
        return ApiResult.Ok(true);
    }
}

/// <summary>Runs the morning job now, e.g. after the server was down at 08:30. It normally runs by itself.</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Jobs")]
public sealed class JobsController(DailyTicketJob job) : ControllerBase
{
    [HttpPost("Morning")]
    [RequireRoles(AppRole.Admin)]
    public async Task<ResultViewModel<DailyJobResult>> Morning(CancellationToken cancellationToken) =>
        ApiResult.Ok(await job.RunAsync(cancellationToken));
}
