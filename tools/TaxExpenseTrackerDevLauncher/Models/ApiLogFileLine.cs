namespace TaxExpenseTrackerDevLauncher.Models;

public sealed record ApiLogFileLine(string Text)
{
    public bool IsError => Text.Contains("|ERROR|", StringComparison.OrdinalIgnoreCase) ||
                           Text.Contains("|FATAL|", StringComparison.OrdinalIgnoreCase);
}