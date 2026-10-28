using FluentValidation;

namespace SWCare.Api.DTO.Req;

/// <summary>Same names as PageInfo in the envelope (Easy Next).</summary>
public abstract class PageReq
{
    public int PageNumber { get; set; } = 1;
    public int PageLimit { get; set; } = 20;
    public int Skip => (PageNumber - 1) * PageLimit;
}

public abstract class PageReqValidator<T> : AbstractValidator<T>
    where T : PageReq
{
    protected PageReqValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageLimit).InclusiveBetween(1, 200);
    }
}
