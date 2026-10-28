using FluentValidation;

namespace SWCare.Api.DTO.Req;

public sealed class UserQueryReq : PageReq
{
    /// <summary>Part of the username, name or e-mail.</summary>
    public string? Search { get; set; }

    public int? DepartmentId { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>The Admin types a first password and tells the user; it must be changed at the first login (Q41).</summary>
public sealed class CreateUserReq
{
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public int DepartmentId { get; set; }
    public bool IsAdmin { get; set; }
    public string Password { get; set; } = "";
}

/// <summary>PUT replaces all fields. The username cannot change.</summary>
public sealed class UpdateUserReq
{
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public int DepartmentId { get; set; }
    public bool IsAdmin { get; set; }

    /// <summary>false = the account cannot log in any more; its name stays on old tickets (Q41).</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>Forgotten password: there is no e-mail, the Admin sets a new one (Q41).</summary>
public sealed class ResetPasswordReq
{
    public string NewPassword { get; set; } = "";
}

public static class UserRules
{
    public const string UsernamePattern = "^[A-Za-z0-9._-]+$";
}

public sealed class UserQueryReqValidator : PageReqValidator<UserQueryReq>;

public sealed class CreateUserReqValidator : AbstractValidator<CreateUserReq>
{
    public CreateUserReqValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50).Matches(UserRules.UsernamePattern)
            .WithMessage("Username may use letters, digits, dot, dash and underscore.");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.Password).ValidNewPassword();
    }
}

public sealed class UpdateUserReqValidator : AbstractValidator<UpdateUserReq>
{
    public UpdateUserReqValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.DepartmentId).GreaterThan(0);
    }
}

public sealed class ResetPasswordReqValidator : AbstractValidator<ResetPasswordReq>
{
    public ResetPasswordReqValidator()
    {
        RuleFor(x => x.NewPassword).ValidNewPassword();
    }
}
