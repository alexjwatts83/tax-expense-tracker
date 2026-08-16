namespace TaxExpenseTrackerDevLauncher.Models;

public static class LauncherLogFilter
{
    public static bool Include(LogLine line, bool showApiLogs, bool showWebLogs, string filter)
    {
        if (line.ServiceId.Equals("api", StringComparison.OrdinalIgnoreCase) && !showApiLogs ||
            line.ServiceId.Equals("web", StringComparison.OrdinalIgnoreCase) && !showWebLogs)
            return false;

        return Matches(line.Text, filter);
    }

    public static bool IncludeFrontend(LogLine line, string filter) =>
        line.ServiceId.Equals("web", StringComparison.OrdinalIgnoreCase) && Matches(line.Text, filter);

    private static bool Matches(string text, string filter) =>
        string.IsNullOrWhiteSpace(filter) || text.Contains(filter, StringComparison.OrdinalIgnoreCase);
}