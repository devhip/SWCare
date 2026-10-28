namespace SWCare.Api.Models;

/// <summary>Q1.</summary>
public enum TicketType
{
    Bug,
    FeatureRequest,
    Testing,
    DataService
}

/// <summary>ทำทันที / ด่วน (1–2 วัน) / ปกติ (ตามคิว).</summary>
public enum TicketPriority
{
    Immediate,
    Urgent,
    Normal
}

/// <summary>Plan 4.2. Closed, Cancelled and Declined are final.</summary>
public enum TicketStatus
{
    PendingApproval,
    Rejected,
    PendingTriage,

    /// <summary>Waiting for the reporter; Ticket.StatusBeforeWaiting is where it goes back to.</summary>
    WaitingInfo,

    Queued,
    InProgress,

    /// <summary>The programmer has finished; waiting for the reporter to close it (จบงาน รอปิด).</summary>
    Resolved,

    Closed,
    Cancelled,

    /// <summary>The programmer lead did not take the ticket (ไม่ดำเนินการ, Q31).</summary>
    Declined
}

/// <summary>One line of the ticket timeline (plan 4.3).</summary>
public enum TicketActivityType
{
    Created,
    Edited,
    Resubmitted,
    Approved,
    AutoApproved,
    Rejected,
    Triaged,
    ClassificationChanged,
    InfoRequested,
    InfoProvided,
    Started,
    Paused,
    Resolved,
    SentBack,
    Closed,
    AutoClosed,
    Cancelled,
    Declined,
    Rescheduled,
    Reassigned,
    WatcherAdded,
    WatcherRemoved,
    LinkAdded,
    Imported
}
