using SWCare.Api.BLC;
using SWCare.Api.Models;

namespace SWCare.Api.DTO.Res;

public sealed record UserBriefRes(int Id, string DisplayName);

public sealed record IdNameRes(int Id, string Name);

public sealed record AttachmentRes(
    long Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    UserBriefRes UploadedBy,
    DateTime CreatedAt);

public sealed record CommentRes(
    long Id,
    UserBriefRes Author,
    string Body,
    DateTime CreatedAt,
    List<AttachmentRes> Attachments);

/// <summary>One timeline line. Data is JSON, e.g. the fields changed by an edit.</summary>
public sealed record ActivityRes(
    long Id,
    TicketActivityType Type,
    TicketStatus? FromStatus,
    TicketStatus? ToStatus,
    UserBriefRes? Actor,
    string? Note,
    string? Data,
    DateTime CreatedAt);

/// <summary>Everything the ticket page shows, with the actions the caller may take now.</summary>
public sealed record TicketDetailRes(
    int Id,
    string TicketNo,
    string? LegacyRef,
    string Title,
    TicketType Type,
    IdNameRes Product,
    string? Version,
    string? CustomerName,
    string Description,
    string? TestedDetails,
    TicketPriority RequestedPriority,
    TicketPriority? ActualPriority,
    TicketStatus Status,
    UserBriefRes Reporter,
    UserBriefRes? Requester,
    IdNameRes Department,
    UserBriefRes? Approver,
    DateTime? ApprovedAt,
    UserBriefRes? Assignee,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedDueDate,
    DateTime ReportedAt,
    DateTime? StartedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    string? Resolution,
    int ReturnCount,
    int RescheduleCount,
    bool NeedsLegacyReview,
    string? LegacyReporterName,
    string? LegacyRequesterName,
    string? LegacyApproverName,
    string? LegacyAssigneeName,
    List<IdNameRes> DeviceModels,
    List<UserBriefRes> Watchers,
    List<AttachmentRes> Attachments,
    List<CommentRes> Comments,
    List<ActivityRes> Activities,
    List<TicketAction> AllowedActions);

/// <summary>For the download endpoint.</summary>
public sealed record AttachmentFile(string FullPath, string FileName, string ContentType);
