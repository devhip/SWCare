using SWCare.Api.Models;

namespace SWCare.Api.DTO.Res;

/// <summary>The logged-in user and the roles the web uses to show menus and buttons.</summary>
public sealed record MeRes(
    int UserId,
    string Username,
    string DisplayName,
    string? Email,
    int DepartmentId,
    string DepartmentName,
    DepartmentKind DepartmentKind,
    bool IsAdmin,
    bool IsDepartmentHead,
    bool IsSupportHead,
    bool IsProgrammer,
    bool IsProgrammerLead,
    bool IsExecutive,
    bool MustChangePassword,
    string Language);

public sealed record LoginRes(
    MeRes User,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken);
