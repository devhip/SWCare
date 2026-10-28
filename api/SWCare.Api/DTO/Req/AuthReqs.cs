using FluentValidation;

namespace SWCare.Api.DTO.Req;

public sealed class LoginReq
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class RefreshTokenReq
{
    public string RefreshToken { get; set; } = "";
}

public sealed class ChangePasswordReq
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public sealed class SetLanguageReq
{
    /// <summary>"th" or "en".</summary>
    public string Language { get; set; } = "th";
}

public static class PasswordRules
{
    public const int MinLength = 8;

    /// <summary>BCrypt reads only the first 72 bytes; 64 characters keeps Thai passwords safe too.</summary>
    public const int MaxLength = 64;

    public static IRuleBuilderOptions<T, string> ValidNewPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MinimumLength(MinLength).MaximumLength(MaxLength);
}

public static class Languages
{
    public static readonly string[] Supported = ["th", "en"];
}

public sealed class LoginReqValidator : AbstractValidator<LoginReq>
{
    public LoginReqValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(PasswordRules.MaxLength);
    }
}

public sealed class RefreshTokenReqValidator : AbstractValidator<RefreshTokenReq>
{
    public RefreshTokenReqValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
    }
}

public sealed class ChangePasswordReqValidator : AbstractValidator<ChangePasswordReq>
{
    public ChangePasswordReqValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(PasswordRules.MaxLength);
        RuleFor(x => x.NewPassword).ValidNewPassword()
            .NotEqual(x => x.CurrentPassword).WithMessage("The new password must differ from the current one.");
    }
}

public sealed class SetLanguageReqValidator : AbstractValidator<SetLanguageReq>
{
    public SetLanguageReqValidator()
    {
        RuleFor(x => x.Language).Must(Languages.Supported.Contains).WithMessage("Language must be th or en.");
    }
}
