namespace TaxExpenseTrackerDevLauncher.Models;

public sealed record LogLine(
    DateTimeOffset Timestamp,
    string ServiceId,
    string Stream,
    string Text)
{
    public bool IsError => Stream.Equals("stderr", StringComparison.OrdinalIgnoreCase);

    public string TimestampText => Timestamp.ToLocalTime().ToString("HH:mm:ss.fff");
}