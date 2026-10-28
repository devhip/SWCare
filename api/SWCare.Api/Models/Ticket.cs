namespace SWCare.Api.Models;

/// <summary>One request to the programmers (plan 6.1).</summary>
public sealed class Ticket : BaseEntity<int>
{
    /// <summary>SW-2026-0001, numbered again from 1 every year (Q36).</summary>
    public string TicketNo { get; set; } = "";

    /// <summary>QP / RQ number from the Google Sheet, for imported tickets.</summary>
    public string? LegacyRef { get; set; }

    public string Title { get; set; } = "";
    public TicketType Type { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string? Version { get; set; }
    public string? CustomerName { get; set; }
    public string Description { get; set; } = "";

    /// <summary>สิ่งที่ทดสอบแล้ว: required when Customer Support reports a Bug or FeatureRequest (Q55).</summary>
    public string? TestedDetails { get; set; }

    public TicketPriority RequestedPriority { get; set; }

    /// <summary>Set by the programmer lead at triage; shown next to the requested one (Q21).</summary>
    public TicketPriority? ActualPriority { get; set; }

    /// <summary>คนแจ้ง: who created the ticket.</summary>
    public int ReporterId { get; set; }
    public User Reporter { get; set; } = null!;

    /// <summary>ผู้ร้องขอ, e.g. the salesperson. Null = the reporter.</summary>
    public int? RequesterId { get; set; }
    public User? Requester { get; set; }

    /// <summary>The reporter's department when the ticket was created; its head approves (Q2).</summary>
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public TicketStatus Status { get; set; }
    public TicketStatus? StatusBeforeWaiting { get; set; }

    public int? ApproverId { get; set; }
    public User? Approver { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public int? TriagedById { get; set; }
    public User? TriagedBy { get; set; }
    public DateTime? TriagedAt { get; set; }

    public int? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public DateOnly? PlannedStartDate { get; set; }

    /// <summary>วันคาดว่าจะเสร็จ = the deadline (Q21).</summary>
    public DateOnly? PlannedDueDate { get; set; }

    /// <summary>Position in the assignee's queue; the lead drags to change it (Q30).</summary>
    public int QueueOrder { get; set; }

    public DateTime ReportedAt { get; set; }

    /// <summary>First time it went to InProgress. Once set, the reporter can no longer cancel (Q40).</summary>
    public DateTime? StartedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    /// <summary>When it reached a final status (Closed, Cancelled or Declined).</summary>
    public DateTime? ClosedAt { get; set; }
    public int? ClosedById { get; set; }
    public User? ClosedBy { get; set; }

    /// <summary>การแก้ไข: required when the programmer finishes.</summary>
    public string? Resolution { get; set; }

    /// <summary>Times Support sent it back after the programmer finished (Q16).</summary>
    public int ReturnCount { get; set; }

    public int RescheduleCount { get; set; }

    /// <summary>Imported from the sheet with dates or data that do not add up (Q43).</summary>
    public bool NeedsLegacyReview { get; set; }

    /// <summary>Names from the sheet that match no account (Q43).</summary>
    public string? LegacyReporterName { get; set; }
    public string? LegacyRequesterName { get; set; }
    public string? LegacyApproverName { get; set; }
    public string? LegacyAssigneeName { get; set; }

    /// <summary>Optimistic concurrency: two people changing the same ticket at once.</summary>
    public byte[] RowVersion { get; set; } = [];

    public ICollection<TicketDeviceModel> DeviceModels { get; set; } = [];
    public ICollection<TicketWatcher> Watchers { get; set; } = [];
    public ICollection<TicketActivity> Activities { get; set; } = [];
    public ICollection<TicketComment> Comments { get; set; } = [];
    public ICollection<TicketAttachment> Attachments { get; set; } = [];
    public ICollection<TicketScheduleChange> ScheduleChanges { get; set; } = [];
}
