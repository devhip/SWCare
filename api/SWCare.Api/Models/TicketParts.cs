namespace SWCare.Api.Models;

public sealed class TicketDeviceModel
{
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public int DeviceModelId { get; set; }
    public DeviceModel DeviceModel { get; set; } = null!;
}

/// <summary>ผู้ติดตาม: sees the ticket and gets its notifications, cannot change its status (Q39).</summary>
public sealed class TicketWatcher : IAuditable
{
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>One timeline line: a status change or an edit (plan 4.3).</summary>
public sealed class TicketActivity : BaseEntity<long>
{
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public TicketActivityType Type { get; set; }
    public TicketStatus? FromStatus { get; set; }
    public TicketStatus? ToStatus { get; set; }

    /// <summary>Null = done by the system (auto approve, auto close, import).</summary>
    public int? ActorId { get; set; }
    public User? Actor { get; set; }

    /// <summary>Reason / request text typed by the user.</summary>
    public string? Note { get; set; }

    /// <summary>JSON with the changed fields, old and new values.</summary>
    public string? Data { get; set; }
}

/// <summary>History of the planned dates (Q23).</summary>
public sealed class TicketScheduleChange : BaseEntity<long>
{
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public DateOnly? OldStartDate { get; set; }
    public DateOnly? OldDueDate { get; set; }
    public DateOnly NewStartDate { get; set; }
    public DateOnly NewDueDate { get; set; }
    public string Reason { get; set; } = "";
    public int ChangedById { get; set; }
    public User ChangedBy { get; set; } = null!;
}

/// <summary>Everyone who can see the ticket sees every comment (Q47).</summary>
public sealed class TicketComment : BaseEntity<long>
{
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public string Body { get; set; } = "";
    public ICollection<TicketAttachment> Attachments { get; set; } = [];
}

/// <summary>A file on the server disk, served only through the API after a permission check (Q20, plan 10).</summary>
public sealed class TicketAttachment : BaseEntity<long>
{
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    /// <summary>Null = attached when the ticket was created.</summary>
    public long? CommentId { get; set; }
    public TicketComment? Comment { get; set; }

    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }

    /// <summary>Name of the file in the attachments folder (a GUID, never the user's file name).</summary>
    public string StoredName { get; set; } = "";

    public int UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;
}

/// <summary>งานที่เกี่ยวข้อง. One row per pair, shown on both tickets.</summary>
public sealed class TicketLink : BaseEntity<long>
{
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public int LinkedTicketId { get; set; }
    public Ticket LinkedTicket { get; set; } = null!;
}

/// <summary>Last running number per year for TicketNo.</summary>
public sealed class TicketSequence
{
    public int Year { get; set; }
    public int LastNumber { get; set; }
}
