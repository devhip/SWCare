using FluentValidation;
using SWCare.Api.Models;

namespace SWCare.Api.DTO.Req;

/// <summary>The details the reporter fills in (plan 6.1). PUT replaces all of them.</summary>
public sealed class SaveTicketReq
{
    public string Title { get; set; } = "";
    public TicketType Type { get; set; }
    public int ProductId { get; set; }
    public string? Version { get; set; }
    public string? CustomerName { get; set; }
    public string Description { get; set; } = "";

    /// <summary>สิ่งที่ทดสอบแล้ว: required when Customer Support reports a Bug or FeatureRequest (Q55).</summary>
    public string? TestedDetails { get; set; }

    public TicketPriority RequestedPriority { get; set; } = TicketPriority.Normal;

    /// <summary>ผู้ร้องขอ, e.g. the salesperson. Null = the reporter.</summary>
    public int? RequesterId { get; set; }

    public List<int> DeviceModelIds { get; set; } = [];
}

/// <summary>
/// POST api/Tickets is multipart so the files arrive with the ticket: a department head's own ticket is approved
/// at once and could not take files afterwards.
/// </summary>
public sealed class CreateTicketForm
{
    /// <summary>SaveTicketReq as JSON.</summary>
    public string Ticket { get; set; } = "";

    public List<IFormFile> Files { get; set; } = [];
}

public sealed class AddFilesForm
{
    public List<IFormFile> Files { get; set; } = [];
}

public sealed class AddCommentForm
{
    public string? Body { get; set; }
    public List<IFormFile> Files { get; set; } = [];
}

public sealed class SaveTicketReqValidator : AbstractValidator<SaveTicketReq>
{
    public SaveTicketReqValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Version).MaximumLength(50);
        RuleFor(x => x.CustomerName).MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(20000);
        RuleFor(x => x.TestedDetails).MaximumLength(8000);
        RuleFor(x => x.RequestedPriority).IsInEnum();
        RuleFor(x => x.DeviceModelIds).Must(ids => ids.Count <= 20).WithMessage("At most 20 device models.");
    }
}

public sealed class CreateTicketFormValidator : AbstractValidator<CreateTicketForm>
{
    public CreateTicketFormValidator()
    {
        RuleFor(x => x.Ticket).NotEmpty();
    }
}

public sealed class AddCommentFormValidator : AbstractValidator<AddCommentForm>
{
    public AddCommentFormValidator()
    {
        RuleFor(x => x.Body).MaximumLength(8000);
    }
}
