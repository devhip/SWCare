namespace SWCare.Api.Models;

/// <summary>What a department means to the ticket flow (plan 3).</summary>
public enum DepartmentKind
{
    General,

    /// <summary>Customer Support: its head sees and is notified about every ticket (Q14).</summary>
    Support,

    /// <summary>The programmers. Its head is the programmer lead who triages every ticket (Q34).</summary>
    Software
}

public sealed class Department : BaseEntity<int>
{
    public string Name { get; set; } = "";
    public DepartmentKind Kind { get; set; }

    /// <summary>The only approver of tickets reported by this department's members (Q2, Q14). Must be a member.</summary>
    public int? HeadUserId { get; set; }
    public User? HeadUser { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<User> Members { get; set; } = [];
}
