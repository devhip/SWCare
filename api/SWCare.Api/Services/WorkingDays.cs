namespace SWCare.Api.Services;

/// <summary>Monday to Friday; public holidays are not counted yet (Q47, plan 17).</summary>
public static class WorkingDays
{
    public static bool IsWorkingDay(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    /// <summary>The date <paramref name="days"/> working days after <paramref name="start"/>.</summary>
    public static DateOnly Add(DateOnly start, int days)
    {
        var date = start;
        for (var added = 0; added < days;)
        {
            date = date.AddDays(1);
            if (IsWorkingDay(date))
            {
                added++;
            }
        }

        return date;
    }

    /// <summary>
    /// A finished ticket closes itself on the morning of this date if the reporter has not closed it (Q16, Q27):
    /// <paramref name="days"/> working days after the programmer finished, in Thai time.
    /// </summary>
    public static DateOnly AutoCloseDate(DateTime resolvedAtUtc, int days) =>
        Add(DateOnly.FromDateTime(ThaiTime.FromUtc(resolvedAtUtc)), days);
}
