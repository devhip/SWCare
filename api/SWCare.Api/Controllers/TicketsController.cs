using System.Text.Json;
using FluentValidation;
using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace SWCare.Api.Controllers;

/// <summary>Tickets (งาน). The status actions (approve, triage, ...) come with day 3 of the plan.</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Tickets")]
public sealed class TicketsController(
    ITicketControl control,
    ITicketActionControl actions,
    IValidator<SaveTicketReq> validator,
    IOptions<JsonOptions> jsonOptions) : ControllerBase
{
    /// <summary>Tickets the caller can see. Scope picks a list: Reported, Following, ToDo, ToApprove, ToClose, ToTriage.</summary>
    [HttpGet]
    public async Task<ResultsViewModel<TicketListItemRes>> List([FromQuery] TicketQueryReq request, CancellationToken cancellationToken) =>
        ApiResult.OkPage(await control.ListAsync(request, cancellationToken), request);

    /// <summary>Open tickets per list, for the badges.</summary>
    [HttpGet("Counts")]
    public async Task<ResultViewModel<TicketCountsRes>> Counts(CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.CountsAsync(cancellationToken));

    /// <summary>
    /// multipart/form-data: "Ticket" = SaveTicketReq as JSON, "Files" = up to 10 files of 20 MB.
    /// 422 TESTED_DETAILS_REQUIRED, NO_DEPARTMENT_HEAD, FILE_TOO_LARGE, FILE_TYPE_NOT_ALLOWED ...
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(AttachmentRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = AttachmentRules.MaxRequestBytes)]
    public async Task<ResultViewModel<TicketDetailRes>> Create([FromForm] CreateTicketForm form, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<SaveTicketReq>(form.Ticket, jsonOptions.Value.JsonSerializerOptions)
                      ?? throw new JsonException("Ticket is empty.");
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return ApiResult.Ok(await control.CreateAsync(request, form.Files, cancellationToken));
    }

    /// <summary>404 when the caller may not see it. AllowedActions = the buttons to show.</summary>
    [HttpGet("{id:int}")]
    public async Task<ResultViewModel<TicketDetailRes>> Get(int id, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.GetAsync(id, cancellationToken));

    /// <summary>Reporter only, until approved (403 ACTION_NOT_ALLOWED after that).</summary>
    [HttpPut("{id:int}")]
    public async Task<ResultViewModel<TicketDetailRes>> Update(int id, SaveTicketReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.UpdateAsync(id, request, cancellationToken));

    /// <summary>Files on the ticket itself, while the reporter can still edit it.</summary>
    [HttpPost("{id:int}/Attachments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(AttachmentRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = AttachmentRules.MaxRequestBytes)]
    public async Task<ResultViewModel<TicketDetailRes>> AddAttachments(int id, [FromForm] AddFilesForm form, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.AddAttachmentsAsync(id, form.Files, cancellationToken));

    [HttpGet("{id:int}/Attachments/{attachmentId:long}")]
    public async Task<IActionResult> DownloadAttachment(int id, long attachmentId, CancellationToken cancellationToken)
    {
        var file = await control.OpenAttachmentAsync(id, attachmentId, cancellationToken);
        return PhysicalFile(file.FullPath, file.ContentType, file.FileName);
    }

    /// <summary>multipart/form-data: "Body" and/or "Files".</summary>
    [HttpPost("{id:int}/Comments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(AttachmentRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = AttachmentRules.MaxRequestBytes)]
    public async Task<ResultViewModel<TicketDetailRes>> AddComment(int id, [FromForm] AddCommentForm form, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.AddCommentAsync(id, form, cancellationToken));

    // Status changes (plan 4.3). 403 ACTION_NOT_ALLOWED when the caller may not do it in the current status.

    [HttpPost("{id:int}/Approve")]
    public async Task<ResultViewModel<TicketDetailRes>> Approve(int id, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.ApproveAsync(id, cancellationToken));

    [HttpPost("{id:int}/Reject")]
    public async Task<ResultViewModel<TicketDetailRes>> Reject(int id, ReasonReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.RejectAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/Resubmit")]
    public async Task<ResultViewModel<TicketDetailRes>> Resubmit(int id, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.ResubmitAsync(id, cancellationToken));

    [HttpPost("{id:int}/Cancel")]
    public async Task<ResultViewModel<TicketDetailRes>> Cancel(int id, ReasonReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.CancelAsync(id, request, cancellationToken));

    /// <summary>422 ASSIGNEE_NOT_PROGRAMMER, INACTIVE_PRODUCT, INACTIVE_DEVICE_MODEL.</summary>
    [HttpPost("{id:int}/Triage")]
    public async Task<ResultViewModel<TicketDetailRes>> Triage(int id, TriageReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.TriageAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/RequestInfo")]
    public async Task<ResultViewModel<TicketDetailRes>> RequestInfo(int id, ReasonReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.RequestInfoAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/Decline")]
    public async Task<ResultViewModel<TicketDetailRes>> Decline(int id, ReasonReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.DeclineAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/Start")]
    public async Task<ResultViewModel<TicketDetailRes>> Start(int id, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.StartAsync(id, cancellationToken));

    [HttpPost("{id:int}/Pause")]
    public async Task<ResultViewModel<TicketDetailRes>> Pause(int id, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.PauseAsync(id, cancellationToken));

    [HttpPost("{id:int}/Resolve")]
    public async Task<ResultViewModel<TicketDetailRes>> Resolve(int id, ResolveReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.ResolveAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/Close")]
    public async Task<ResultViewModel<TicketDetailRes>> Close(int id, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.CloseAsync(id, cancellationToken));

    [HttpPost("{id:int}/SendBack")]
    public async Task<ResultViewModel<TicketDetailRes>> SendBack(int id, ReasonReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.SendBackAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/Reschedule")]
    public async Task<ResultViewModel<TicketDetailRes>> Reschedule(int id, RescheduleReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.RescheduleAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/Reassign")]
    public async Task<ResultViewModel<TicketDetailRes>> Reassign(int id, ReassignReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.ReassignAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/Watchers")]
    public async Task<ResultViewModel<TicketDetailRes>> AddWatcher(int id, WatcherReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.AddWatcherAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}/Watchers/{userId:int}")]
    public async Task<ResultViewModel<TicketDetailRes>> RemoveWatcher(int id, int userId, CancellationToken cancellationToken) =>
        ApiResult.Ok(await actions.RemoveWatcherAsync(id, userId, cancellationToken));

    /// <summary>Customer names used on tickets the caller can see.</summary>
    [HttpGet("CustomerNames")]
    public async Task<ResultsViewModel<string>> CustomerNames([FromQuery] string? search, CancellationToken cancellationToken) =>
        ApiResult.OkList(await control.CustomerNamesAsync(search, cancellationToken));
}
