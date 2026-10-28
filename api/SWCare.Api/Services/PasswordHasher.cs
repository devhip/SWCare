namespace SWCare.Api.Services;

/// <summary>BCrypt, as in Easy Next.</summary>
public static class PasswordHasher
{
    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

    public static bool Verify(string password, string? hash) =>
        hash is not null && BCrypt.Net.BCrypt.Verify(password, hash);
}
