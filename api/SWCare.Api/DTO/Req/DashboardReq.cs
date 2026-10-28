using FluentValidation;

namespace SWCare.Api.DTO.Req;

/// <summary>Filters of the Dashboard and its Excel export. Dates are Thai calendar days, both included.</summary>
public sealed class DashboardReq
{
    /// <summary>Empty = the first day of the month five months ago (six months with this one).</summary>
    public DateOnly? From { get; set; }

    /// <summary>Empty = today.</summary>
    public DateOnly? To { get; set; }

    /// <summary>Ignored for a department head, who always sees their own department (Q46).</summary>
    public int? DepartmentId { get; set; }

    public int? ProductId { get; set; }
}

public sealed class DashboardReqValidator : AbstractValidator<DashboardReq>
{
    public DashboardReqValidator()
    {
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).When(x => x.From is not null && x.To is not null);
        RuleFor(x => x).Must(x => x.From is null || x.To is null || x.To.Value.DayNumber - x.From.Value.DayNumber <= 3 * 366)
            .WithMessage("The range can be at most 3 years.");
    }
}
