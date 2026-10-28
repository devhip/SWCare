namespace SWCare.Api.BLC;

/// <summary>Errors that map to an HTTP status and the envelope { Success = false, Message, Code } (see ApiExceptionHandler).</summary>
/// <param name="code">Machine-readable code. The web shows its own text for it (locales/*/errors.json); Message is the fallback.</param>
public abstract class ApiException(string message, string code) : Exception(message)
{
    public abstract int StatusCode { get; }
    public string Code { get; } = code;
}

public sealed class UnauthorizedException(string message, string code) : ApiException(message, code)
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
}

public sealed class ForbiddenException(string message, string code = ErrorCodes.Forbidden) : ApiException(message, code)
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
}

public sealed class NotFoundException(string message, string code = ErrorCodes.NotFound) : ApiException(message, code)
{
    public override int StatusCode => StatusCodes.Status404NotFound;
}

public sealed class ConflictException(string message, string code) : ApiException(message, code)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
}

/// <summary>The request is valid but breaks a rule, e.g. a department head who is not a member.</summary>
public sealed class BusinessRuleException(string message, string code) : ApiException(message, code)
{
    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;
}

/// <summary>Every code the API returns. Keep web/locales/*/errors.json in step.</summary>
public static class ErrorCodes
{
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string ServerError = "SERVER_ERROR";

    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string AccountDisabled = "ACCOUNT_DISABLED";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    public const string PasswordChangeRequired = "PASSWORD_CHANGE_REQUIRED";
    public const string WrongCurrentPassword = "WRONG_CURRENT_PASSWORD";
    public const string PasswordSameAsUsername = "PASSWORD_SAME_AS_USERNAME";

    public const string UsernameTaken = "USERNAME_TAKEN";
    public const string EmailTaken = "EMAIL_TAKEN";
    public const string NameTaken = "NAME_TAKEN";
    public const string CannotChangeOwnAccess = "CANNOT_CHANGE_OWN_ACCESS";
    public const string UserIsDepartmentHead = "USER_IS_DEPARTMENT_HEAD";
    public const string UserOwnsProducts = "USER_OWNS_PRODUCTS";
    public const string InactiveDepartment = "INACTIVE_DEPARTMENT";

    public const string HeadNotMember = "HEAD_NOT_MEMBER";
    public const string DepartmentHasMembers = "DEPARTMENT_HAS_MEMBERS";
    public const string DepartmentKindLocked = "DEPARTMENT_KIND_LOCKED";
    public const string OwnerNotProgrammer = "OWNER_NOT_PROGRAMMER";

    public const string ActionNotAllowed = "ACTION_NOT_ALLOWED";
    public const string NoDepartmentHead = "NO_DEPARTMENT_HEAD";
    public const string TestedDetailsRequired = "TESTED_DETAILS_REQUIRED";
    public const string InactiveProduct = "INACTIVE_PRODUCT";
    public const string InactiveDeviceModel = "INACTIVE_DEVICE_MODEL";
    public const string InactiveUser = "INACTIVE_USER";
    public const string EmptyComment = "EMPTY_COMMENT";
    public const string TooManyFiles = "TOO_MANY_FILES";
    public const string FileTooLarge = "FILE_TOO_LARGE";
    public const string FileTypeNotAllowed = "FILE_TYPE_NOT_ALLOWED";
}
