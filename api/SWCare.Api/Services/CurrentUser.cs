using SWCare.Api.Models;

namespace SWCare.Api.Services;

/// <summary>Roles that follow from the account and its department (plan 3).</summary>
public enum AppRole
{
    Admin,
    DepartmentHead,
    SupportHead,
    Programmer,
    ProgrammerLead,

    /// <summary>ผู้บริหาร (D10).</summary>
    Executive
}

/// <summary>The logged-in user as the business rules see it.</summary>
public sealed record UserContext(
    int Id,
    string Username,
    string DisplayName,
    int DepartmentId,
    string DepartmentName,
    DepartmentKind DepartmentKind,
    bool IsAdmin,
    bool IsDepartmentHead,
    bool MustChangePassword,
    string Language,
    bool IsExecutive = false)
{
    /// <summary>Members of the Software department are the programmers.</summary>
    public bool IsProgrammer => DepartmentKind == DepartmentKind.Software;

    /// <summary>หัวหน้าโปรแกรมเมอร์ = head of the Software department (Q34).</summary>
    public bool IsProgrammerLead => IsProgrammer && IsDepartmentHead;

    /// <summary>หัวหน้า Support sees every ticket (Q14).</summary>
    public bool IsSupportHead => DepartmentKind == DepartmentKind.Support && IsDepartmentHead;

    /// <summary>Sees every ticket only because of the executive flag: read-only there (D10).</summary>
    public bool IsExecutiveOnly => IsExecutive && !IsAdmin && !IsSupportHead && !IsProgrammer;

    public bool Has(AppRole role) => role switch
    {
        AppRole.Admin => IsAdmin,
        AppRole.DepartmentHead => IsDepartmentHead,
        AppRole.SupportHead => IsSupportHead,
        AppRole.Programmer => IsProgrammer,
        AppRole.ProgrammerLead => IsProgrammerLead,
        AppRole.Executive => IsExecutive,
        _ => false
    };
}

/// <summary>The user of the current request, set by CurrentUserFilter.</summary>
public sealed class CurrentUser
{
    private UserContext? _user;

    public UserContext User => _user ?? throw new InvalidOperationException("No logged-in user in this request.");

    public void Set(UserContext user) => _user = user;
}
