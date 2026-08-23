namespace TaxExpenseTracker.Application.Common;

public enum SummaryView
{
    Week,
    Month,
}

public sealed record HolidayMarkerDto(
    DateOnly Date,
    string Name,
    bool CanBeWorkedOn);

public sealed record DayEntrySummaryDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal TotalHours,
    int TotalDays,
    int EntryCount,
    IReadOnlyList<HolidayMarkerDto> Holidays);

public static class SummaryPeriod
{
    public static (DateOnly FromDate, DateOnly ToDate) GetBounds(DateOnly anchorDate, SummaryView view)
    {
        return view switch
        {
            SummaryView.Week => GetWeekBounds(anchorDate),
            SummaryView.Month => GetMonthBounds(anchorDate),
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Unsupported summary view."),
        };
    }

    private static (DateOnly FromDate, DateOnly ToDate) GetWeekBounds(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        var fromDate = date.AddDays(-offset);
        var toDate = fromDate.AddDays(6);
        return (fromDate, toDate);
    }

    private static (DateOnly FromDate, DateOnly ToDate) GetMonthBounds(DateOnly date)
    {
        var fromDate = new DateOnly(date.Year, date.Month, 1);
        var toDate = fromDate.AddMonths(1).AddDays(-1);
        return (fromDate, toDate);
    }
}