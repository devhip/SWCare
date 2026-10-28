using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SWCare.Api.Services;

/// <summary>"Jwt" section. Key comes from user-secrets (dev) or the Jwt__Key environment variable (server).</summary>
public sealed record JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "SWCare.Api";
    public string Audience { get; init; } = "SWCare";
    public string Key { get; init; } = "";
    public int AccessTokenMinutes { get; init; } = 60;
    public int RefreshTokenDays { get; init; } = 14;

    public static JwtOptions Load(IConfiguration configuration, IHostEnvironment environment)
    {
        var options = configuration.GetSection(SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(options.Key))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException("Jwt:Key is missing. Set the Jwt__Key environment variable (32+ characters).");
            }

            // Development only: a new key on every start. Old access tokens stop working and the web
            // simply gets new ones with its refresh token, which lives in the database.
            options = options with { Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)) };
        }

        if (Encoding.UTF8.GetByteCount(options.Key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");
        }

        return options;
    }

    public SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(Key));

    public TokenValidationParameters ValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = SigningKey(),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
}
