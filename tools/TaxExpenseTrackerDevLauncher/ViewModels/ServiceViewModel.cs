using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxExpenseTrackerDevLauncher.Models;
using TaxExpenseTrackerDevLauncher.Services;

namespace TaxExpenseTrackerDevLauncher.ViewModels;

public sealed class ServiceViewModel : ObservableObject
{
    private readonly ProcessSupervisor _supervisor;
    private ServiceState _state;
    private int? _processId;
    private DateTimeOffset? _startedAt;
    private int? _exitCode;
    private string _uptime = "--";
    private IReadOnlyList<PortOwner> _portConflicts = [];

    public ServiceViewModel(ServiceDefinition definition, ProcessSupervisor supervisor)
    {
        Definition = definition;
        _supervisor = supervisor;
        StartCommand = new AsyncRelayCommand(StartAsync, CanStart);
        StopCommand = new AsyncRelayCommand(StopAsync, CanStop);
        RestartCommand = new AsyncRelayCommand(RestartAsync, CanRestart);
        ForceFreePortCommand = new AsyncRelayCommand(ForceFreePortsAsync, () => HasPortConflict);
    }

    public ServiceDefinition Definition { get; }
    public string Id => Definition.Id;
    public string DisplayName => Definition.DisplayName;
    public string LocalUrl => Definition.LocalUri.ToString();
    public string PortsText => string.Join(", ", Definition.Ports);

    public ServiceState State
    {
        get => _state;
        private set
        {
            if (!SetProperty(ref _state, value))
                return;

            OnPropertyChanged(nameof(StateText));
            NotifyCommandStates();
        }
    }

    public string StateText => State.ToString();

    public int? ProcessId
    {
        get => _processId;
        private set
        {
            if (SetProperty(ref _processId, value))
                OnPropertyChanged(nameof(ProcessText));
        }
    }

    public string ProcessText => ProcessId?.ToString() ?? "--";

    public int? ExitCode
    {
        get => _exitCode;
        private set => SetProperty(ref _exitCode, value);
    }

    public string Uptime
    {
        get => _uptime;
        private set => SetProperty(ref _uptime, value);
    }

    public IAsyncRelayCommand StartCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IAsyncRelayCommand RestartCommand { get; }
    public IAsyncRelayCommand ForceFreePortCommand { get; }
    public bool HasPortConflict => PortConflicts.Count > 0;
    public string PortConflictText => string.Join("; ", PortConflicts.Select(owner =>
        $"Port {owner.Port}: {owner.ProcessName} (PID {owner.ProcessId})"));

    public IReadOnlyList<PortOwner> PortConflicts
    {
        get => _portConflicts;
        private set
        {
            if (!SetProperty(ref _portConflicts, value))
                return;

            OnPropertyChanged(nameof(HasPortConflict));
            OnPropertyChanged(nameof(PortConflictText));
            ForceFreePortCommand.NotifyCanExecuteChanged();
        }
    }

    public Task StartAsync() => _supervisor.StartAsync(Id);
    public Task StopAsync() => _supervisor.StopAsync(Id);
    public Task RestartAsync() => _supervisor.RestartAsync(Id);

    public void ApplyPortConflicts(IReadOnlyList<PortOwner> owners) => PortConflicts = owners;

    public void ApplyStatus(ServiceStatusChangedEventArgs status)
    {
        State = status.State;
        ProcessId = status.State is ServiceState.Stopped ? null : status.ProcessId ?? ProcessId;
        _startedAt = status.StartedAt ?? _startedAt;
        ExitCode = status.ExitCode;

        if (status.State is ServiceState.Stopped or ServiceState.Crashed)
        {
            if (status.State == ServiceState.Stopped)
                ProcessId = null;

            _startedAt = null;
            Uptime = "--";
        }

        if (status.State is ServiceState.Starting or ServiceState.Running)
            PortConflicts = [];
    }

    public void RefreshUptime(DateTimeOffset now)
    {
        if (_startedAt is null || State is ServiceState.Stopped or ServiceState.Crashed)
            return;

        var elapsed = now - _startedAt.Value;
        Uptime = elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
            : $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }

    private bool CanStart() => State is ServiceState.Stopped or ServiceState.Crashed;
    private bool CanStop() => State is ServiceState.Starting or ServiceState.Running;
    private bool CanRestart() => State is ServiceState.Running or ServiceState.Crashed;

    private void NotifyCommandStates()
    {
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        RestartCommand.NotifyCanExecuteChanged();
    }

    private async Task ForceFreePortsAsync()
    {
        var currentOwners = PortInspector.GetOwners(Definition.Ports);
        if (currentOwners.Count == 0)
        {
            PortConflicts = [];
            await StartAsync();
            return;
        }

        var processList = string.Join(Environment.NewLine, currentOwners.Select(owner =>
            $"Port {owner.Port}: {owner.ProcessName} (PID {owner.ProcessId})"));
        var confirmation = MessageBox.Show(
            $"Terminate the following external process tree(s)?{Environment.NewLine}{Environment.NewLine}{processList}",
            $"Free {DisplayName} port",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes)
            return;

        try
        {
            if (currentOwners.Any(owner => owner.ProcessStartedAt is null))
                throw new InvalidOperationException("The current process identity could not be verified, so it was not terminated.");

            var confirmedOwners = currentOwners
                .Select(owner => (owner.Port, owner.ProcessId, owner.ProcessStartedAt))
                .ToHashSet();
            var verifiedOwners = PortInspector.GetOwners(Definition.Ports);
            var verifiedOwnerKeys = verifiedOwners
                .Select(owner => (owner.Port, owner.ProcessId, owner.ProcessStartedAt))
                .ToHashSet();
            if (!confirmedOwners.SetEquals(verifiedOwnerKeys))
            {
                PortConflicts = verifiedOwners;
                MessageBox.Show(
                    "Port ownership changed while confirmation was open. Review the current owner before trying again.",
                    $"Free {DisplayName} port",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            foreach (var owner in verifiedOwners.DistinctBy(owner => owner.ProcessId))
                await PortInspector.KillProcessTreeAsync(owner);

            var remainingOwners = PortInspector.GetOwners(Definition.Ports);
            if (remainingOwners.Count > 0)
            {
                PortConflicts = remainingOwners;
                MessageBox.Show(
                    "One or more ports are still occupied. Review the current owner before trying again.",
                    $"Free {DisplayName} port",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
        }
        catch (Exception exception)
        {
            try
            {
                PortConflicts = PortInspector.GetOwners(Definition.Ports);
            }
            catch
            {
                PortConflicts = [];
            }

            MessageBox.Show(
                $"Could not free the selected port: {exception.Message}",
                $"Free {DisplayName} port",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        PortConflicts = [];
        await StartAsync();
    }
}