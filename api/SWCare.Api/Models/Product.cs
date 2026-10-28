namespace SWCare.Api.Models;

/// <summary>A system / program that tickets are about (ระบบ/โปรแกรม, plan 6.2).</summary>
public sealed class Product : BaseEntity<int>
{
    public string Name { get; set; } = "";

    /// <summary>Main programmer: suggested as the assignee when the lead triages (Q3, Q19). Null = the lead picks.</summary>
    public int? OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
