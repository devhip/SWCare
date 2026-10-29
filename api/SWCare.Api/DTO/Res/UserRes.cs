namespace SWCare.Api.DTO.Res;

public sealed record UserRes(
    int Id,
    string Username,
    string DisplayName,
    string? Email,
    int DepartmentId,
    string DepartmentName,
    bool IsDepartmentHead,
    bool IsAdmin,
    bool IsExecutive,
    bool CanViewAllTickets,
    bool IsActive,
    bool MustChangePassword,
    string Language,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>For pickers such as ผู้ร้องขอ and ผู้ติดตาม.</summary>
public sealed record UserLookupRes(
    int Id,
    string DisplayName,
    int DepartmentId,
    string DepartmentName);
