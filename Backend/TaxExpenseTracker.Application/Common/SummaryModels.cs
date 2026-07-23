namespace TaxExpenseTracker.Application.Common;

public enum SummaryView
{
    Week,
    Month,
}

public sealed record HolidayMarkerDto(
    DateTime Date,
    string Name);

public sealed record DayEntrySummaryDto(
    DateTime FromDate,
    DateTime ToDate,
    decimal TotalHours,
    int TotalDays,
    int EntryCount,
    IReadOnlyList<HolidayMarkerDto> Holidays);

public static class SummaryPeriod
{
    public static (DateTime FromDate, DateTime ToDate) GetBounds(DateTime anchorDate, SummaryView view)
    {
        var date = anchorDate.Date;

        return view switch
        {
            SummaryView.Week => GetWeekBounds(date),
            SummaryView.Month => GetMonthBounds(date),
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Unsupported summary view."),
        };
    }

    private static (DateTime FromDate, DateTime ToDate) GetWeekBounds(DateTime date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        var fromDate = date.AddDays(-offset);
        var toDate = fromDate.AddDays(6);
        return (fromDate, toDate);
    }

    private static (DateTime FromDate, DateTime ToDate) GetMonthBounds(DateTime date)
    {
        var fromDate = new DateTime(date.Year, date.Month, 1);
        var toDate = fromDate.AddMonths(1).AddDays(-1);
        return (fromDate, toDate);
    }
}