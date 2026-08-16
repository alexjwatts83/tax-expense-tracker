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

    public ServiceViewModel(ServiceDefinition definition, ProcessSupervisor supervisor)
    {
        Definition = definition;
        _supervisor = supervisor;
        StartCommand = new AsyncRelayCommand(StartAsync, CanStart);
        StopCommand = new AsyncRelayCommand(StopAsync, CanStop);
        RestartCommand = new AsyncRelayCommand(RestartAsync, CanRestart);
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

    public Task StartAsync() => _supervisor.StartAsync(Id);
    public Task StopAsync() => _supervisor.StopAsync(Id);
    public Task RestartAsync() => _supervisor.RestartAsync(Id);

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
}