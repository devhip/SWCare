namespace SWCare.Api.Models;

/// <summary>
/// An account. Roles are not stored: programmer, department head, Support head and programmer lead
/// all follow from the department (see UserContext); only Admin is a flag.
/// </summary>
public sealed class User : BaseEntity<int>
{
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";

    /// <summary>Company e-mail. Also the key for Google sign-in in week 2 (plan 8).</summary>
    public string? Email { get; set; }

    /// <summary>BCrypt hash. Null only for a Google-only account (week 2).</summary>
    public string? PasswordHash { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public bool IsAdmin { get; set; }

    /// <summary>ผู้บริหาร: sees the whole-company Dashboard and every ticket, read-only (D10).</summary>
    public bool IsExecutive { get; set; }

    /// <summary>ดูงานทั้งหมด: searches and reads every ticket, e.g. a salesperson looking up a customer's history (D14).</summary>
    public bool CanViewAllTickets { get; set; }

    /// <summary>A disabled account cannot log in and its refresh tokens are revoked (Q41).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Set for a new account and after an Admin password reset (Q41).</summary>
    public bool MustChangePassword { get; set; } = true;

    /// <summary>"th" or "en" (Q49).</summary>
    public string Language { get; set; } = "th";

    public DateTime? LastLoginAt { get; set; }
}
