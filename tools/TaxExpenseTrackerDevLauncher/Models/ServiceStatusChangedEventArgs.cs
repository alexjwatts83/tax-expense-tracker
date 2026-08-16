namespace TaxExpenseTrackerDevLauncher.Models;

public sealed class ServiceStatusChangedEventArgs(
    string serviceId,
    ServiceState state,
    int? processId = null,
    DateTimeOffset? startedAt = null,
    int? exitCode = null) : EventArgs
{
    public string ServiceId { get; } = serviceId;
    public ServiceState State { get; } = state;
    public int? ProcessId { get; } = processId;
    public DateTimeOffset? StartedAt { get; } = startedAt;
    public int? ExitCode { get; } = exitCode;
}