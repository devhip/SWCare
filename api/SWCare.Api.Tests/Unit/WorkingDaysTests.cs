using SWCare.Api.Services;

namespace SWCare.Api.Tests.Unit;

public sealed class WorkingDaysTests
{
    [Theory]
    [InlineData("2026-10-26", 3, "2026-10-29")] // Monday + 3 = Thursday
    [InlineData("2026-10-30", 3, "2026-11-04")] // Friday + 3 = Wednesday, not Monday (Q27)
    [InlineData("2026-10-31", 1, "2026-11-02")] // Saturday + 1 = Monday
    [InlineData("2026-10-28", 0, "2026-10-28")]
    public void Add_skips_weekends(string start, int days, string expected) =>
        Assert.Equal(DateOnly.Parse(expected), WorkingDays.Add(DateOnly.Parse(start), days));

    [Fact]
    public void Auto_close_date_uses_the_thai_date()
    {
        // 23:30 UTC on Thursday is already Friday 06:30 in Thailand.
        var resolved = new DateTime(2026, 10, 29, 23, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateOnly(2026, 11, 4), WorkingDays.AutoCloseDate(resolved, 3));
    }
}
