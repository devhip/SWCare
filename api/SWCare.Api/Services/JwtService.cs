using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SWCare.Api.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SWCare.Api.Services;

/// <summary>
/// Access tokens carry only who the user is; roles are read from the database on each request (CurrentUserFilter).
/// Refresh tokens are random strings stored as SHA-256.
/// </summary>
public sealed class JwtService(JwtOptions options, TimeProvider clock)
{
    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(options.AccessTokenMinutes);

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ]),
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(options.SigningKey(), SecurityAlgorithms.HmacSha256)
        });

        return (token, expiresAt);
    }

    public (string Token, string Hash, DateTime ExpiresAt) CreateRefreshToken()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48));
        return (token, HashRefreshToken(token), clock.GetUtcNow().UtcDateTime.AddDays(options.RefreshTokenDays));
    }

    public static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
