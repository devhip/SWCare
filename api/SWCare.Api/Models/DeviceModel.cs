namespace SWCare.Api.Models;

/// <summary>A scanner / camera model a ticket can name, several per ticket (Q42).</summary>
public sealed class DeviceModel : BaseEntity<int>
{
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
