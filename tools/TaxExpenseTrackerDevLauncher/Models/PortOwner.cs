namespace TaxExpenseTrackerDevLauncher.Models;

public sealed record PortOwner(int Port, int ProcessId, string ProcessName, DateTimeOffset? ProcessStartedAt);