namespace SWCare.Api.Models;

/// <summary>One login session. Rotated on every refresh; only the SHA-256 of the token is stored.</summary>
public sealed class RefreshToken : BaseEntity<long>
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
