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
    IValidator<SaveTicketReq> validator,
    IOptions<JsonOptions> jsonOptions) : ControllerBase
{
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

    /// <summary>Customer names used on tickets the caller can see.</summary>
    [HttpGet("CustomerNames")]
    public async Task<ResultsViewModel<string>> CustomerNames([FromQuery] string? search, CancellationToken cancellationToken) =>
        ApiResult.OkList(await control.CustomerNamesAsync(search, cancellationToken));
}
