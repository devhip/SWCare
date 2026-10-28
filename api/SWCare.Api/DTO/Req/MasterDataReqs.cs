using System.Globalization;
using FluentValidation;

namespace SWCare.Api.DTO.Req;

/// <summary>A new department is always General; Support and Software exist from the start.</summary>
public sealed class CreateDepartmentReq
{
    public string Name { get; set; } = "";
}

public sealed class UpdateDepartmentReq
{
    public string Name { get; set; } = "";

    /// <summary>Must be an active member of this department. Null = no head (nobody can approve its tickets).</summary>
    public int? HeadUserId { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class SaveProductReq
{
    public string Name { get; set; } = "";

    /// <summary>An active programmer, or null to let the programmer lead pick each time.</summary>
    public int? OwnerUserId { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SaveDeviceModelReq
{
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateSettingsReq
{
    public int AutoCloseWorkingDays { get; set; }

    /// <summary>"HH:mm", Thai time.</summary>
    public string MorningReminderTime { get; set; } = "";

    public string TestedDetailsTemplate { get; set; } = "";
}

public sealed class CreateDepartmentReqValidator : AbstractValidator<CreateDepartmentReq>
{
    public CreateDepartmentReqValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpdateDepartmentReqValidator : AbstractValidator<UpdateDepartmentReq>
{
    public UpdateDepartmentReqValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class SaveProductReqValidator : AbstractValidator<SaveProductReq>
{
    public SaveProductReqValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class SaveDeviceModelReqValidator : AbstractValidator<SaveDeviceModelReq>
{
    public SaveDeviceModelReqValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateSettingsReqValidator : AbstractValidator<UpdateSettingsReq>
{
    public UpdateSettingsReqValidator()
    {
        RuleFor(x => x.AutoCloseWorkingDays).InclusiveBetween(1, 30);
        RuleFor(x => x.MorningReminderTime)
            .Must(value => TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("MorningReminderTime must be HH:mm.");
        RuleFor(x => x.TestedDetailsTemplate).NotEmpty().MaximumLength(4000);
    }
}
