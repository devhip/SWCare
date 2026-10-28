using FluentValidation;
using SWCare.Api.Models;

namespace SWCare.Api.DTO.Req;

/// <summary>Reject, cancel, decline, send back, ask for information: the text goes to the timeline.</summary>
public sealed class ReasonReq
{
    public string Reason { get; set; } = "";
}

public sealed class ResolveReq
{
    /// <summary>การแก้ไข.</summary>
    public string Resolution { get; set; } = "";
}

/// <summary>
/// The programmer lead queues the ticket (Q21) and may correct its classification on the way (Q50):
/// send the current values for fields that do not change.
/// </summary>
public sealed class TriageReq
{
    public TicketPriority ActualPriority { get; set; }
    public int AssigneeId { get; set; }
    public DateOnly PlannedStartDate { get; set; }
    public DateOnly PlannedDueDate { get; set; }

    public int ProductId { get; set; }
    public TicketType Type { get; set; }
    public List<int> DeviceModelIds { get; set; } = [];
    public string? CustomerName { get; set; }
}

public sealed class RescheduleReq
{
    public DateOnly PlannedStartDate { get; set; }
    public DateOnly PlannedDueDate { get; set; }
    public string Reason { get; set; } = "";
}

public sealed class ReassignReq
{
    public int AssigneeId { get; set; }
}

public sealed class WatcherReq
{
    public int UserId { get; set; }
}

/// <summary>The lists of the web: งานของฉัน tabs, รออนุมัติ, รอจัดคิว, รายการทั้งหมด.</summary>
public enum TicketScope
{
    All,

    /// <summary>ที่ฉันแจ้ง.</summary>
    Reported,

    /// <summary>ที่ฉันเป็นผู้ร้องขอหรือผู้ติดตาม.</summary>
    Following,

    /// <summary>ที่ต้องทำ: assigned to me and not finished.</summary>
    ToDo,

    /// <summary>ที่ต้องอนุมัติ: my department's tickets waiting for me as its head.</summary>
    ToApprove,

    /// <summary>ที่รอฉันปิด: finished by the programmer, mine to close.</summary>
    ToClose,

    /// <summary>รอจัดคิว: for the programmer lead.</summary>
    ToTriage
}

public sealed class TicketQueryReq : PageReq
{
    public TicketScope Scope { get; set; } = TicketScope.All;
    public List<TicketStatus> Statuses { get; set; } = [];

    /// <summary>Ticket no, old reference, title, description, fix or customer (Q54).</summary>
    public string? Search { get; set; }

    public int? ProductId { get; set; }
    public int? AssigneeId { get; set; }
    public int? DepartmentId { get; set; }
}

public sealed class ReasonReqValidator : AbstractValidator<ReasonReq>
{
    public ReasonReqValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(4000);
    }
}

public sealed class ResolveReqValidator : AbstractValidator<ResolveReq>
{
    public ResolveReqValidator()
    {
        RuleFor(x => x.Resolution).NotEmpty().MaximumLength(8000);
    }
}

public sealed class TriageReqValidator : AbstractValidator<TriageReq>
{
    public TriageReqValidator()
    {
        RuleFor(x => x.ActualPriority).IsInEnum();
        RuleFor(x => x.AssigneeId).GreaterThan(0);
        RuleFor(x => x.PlannedStartDate).NotEqual(default(DateOnly));
        RuleFor(x => x.PlannedDueDate).NotEqual(default(DateOnly))
            .GreaterThanOrEqualTo(x => x.PlannedStartDate).WithMessage("The expected finish cannot be before the start.");
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.DeviceModelIds).Must(ids => ids.Count <= 20).WithMessage("At most 20 device models.");
        RuleFor(x => x.CustomerName).MaximumLength(200);
    }
}

public sealed class RescheduleReqValidator : AbstractValidator<RescheduleReq>
{
    public RescheduleReqValidator()
    {
        RuleFor(x => x.PlannedStartDate).NotEqual(default(DateOnly));
        RuleFor(x => x.PlannedDueDate).NotEqual(default(DateOnly))
            .GreaterThanOrEqualTo(x => x.PlannedStartDate).WithMessage("The expected finish cannot be before the start.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class ReassignReqValidator : AbstractValidator<ReassignReq>
{
    public ReassignReqValidator()
    {
        RuleFor(x => x.AssigneeId).GreaterThan(0);
    }
}

public sealed class WatcherReqValidator : AbstractValidator<WatcherReq>
{
    public WatcherReqValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}

public sealed class TicketQueryReqValidator : PageReqValidator<TicketQueryReq>
{
    public TicketQueryReqValidator()
    {
        RuleFor(x => x.Scope).IsInEnum();
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
