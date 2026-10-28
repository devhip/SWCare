namespace SWCare.Api.Services;

/// <summary>Dates the users see are in Thai time (Q47); the database stores UTC.</summary>
public static class ThaiTime
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");

    public static DateTime FromUtc(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);

    public static DateTime Now(TimeProvider clock) => FromUtc(clock.GetUtcNow().UtcDateTime);
}
