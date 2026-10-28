namespace SWCare.Api.Models;

/// <summary>What happened. The text is built by the client in the reader's language (Q49) from Type + Data.</summary>
public enum NotificationType
{
    TicketSubmitted,
    TicketApproved,
    TicketRejected,
    TicketTriaged,
    InfoRequested,
    InfoProvided,
    TicketStarted,
    TicketPaused,
    TicketRescheduled,
    TicketReassigned,
    TicketResolved,
    TicketSentBack,
    TicketClosed,
    TicketAutoClosed,
    TicketDeclined,
    TicketCancelled,
    CommentAdded,
    WatcherAdded,
    PendingApprovalReminder,
    AutoCloseReminder,
    OverdueReminder
}

/// <summary>One bell / push item for one user (plan 7).</summary>
public sealed class Notification : BaseEntity<long>
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }

    public int? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    /// <summary>Who caused it; null = the system.</summary>
    public int? ActorId { get; set; }
    public User? Actor { get; set; }

    /// <summary>JSON with the values the message needs (ticket no, new due date ...).</summary>
    public string? Data { get; set; }

    public DateTime? ReadAt { get; set; }
}
