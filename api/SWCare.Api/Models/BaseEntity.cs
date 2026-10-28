namespace SWCare.Api.Models;

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}

/// <summary>
/// Base of most tables: audit columns are filled by AuditSaveChangesInterceptor.
/// Nothing is deleted in SWCare (plan 6.1, Q54): master data is switched off with IsActive
/// and a ticket ends in a final status, so there is no soft-delete filter.
/// </summary>
public abstract class BaseEntity<TKey> : IAuditable
    where TKey : struct
{
    public TKey Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
