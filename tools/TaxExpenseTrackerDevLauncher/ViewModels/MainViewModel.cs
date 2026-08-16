using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TaxExpenseTrackerDevLauncher.Models;
using TaxExpenseTrackerDevLauncher.Services;

namespace TaxExpenseTrackerDevLauncher.ViewModels;

public sealed class MainViewModel : ObservableObject, IAsyncDisposable
{
    private const int MaximumLogLines = 5000;
    private readonly ProcessSupervisor _supervisor;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _uptimeTimer;
    private string _logFilter = string.Empty;
    private bool _showApiLogs = true;
    private bool _showWebLogs = true;
    private bool _autoScroll = true;
    private Uri? _frontendSource;

    public MainViewModel()
    {
        _dispatcher = Application.Current.Dispatcher;
        RepositoryRoot = RepositoryLocator.FindRepositoryRoot();
        var definitions = ServiceRegistry.Create(RepositoryRoot);
        _supervisor = new ProcessSupervisor(definitions);
        Services = new ObservableCollection<ServiceViewModel>(
            definitions.Select(definition => new ServiceViewModel(definition, _supervisor)));

        LogsView = CollectionViewSource.GetDefaultView(LogLines);
        LogsView.Filter = FilterLogLine;

        StartAllCommand = new AsyncRelayCommand(StartAllAsync);
        StopAllCommand = new AsyncRelayCommand(StopAllAsync);
        RestartAllCommand = new AsyncRelayCommand(RestartAllAsync);
        ClearLogsCommand = new RelayCommand(LogLines.Clear);
        SaveLogsCommand = new RelayCommand(SaveLogs);
        OpenFrontendCommand = new RelayCommand(OpenFrontend);

        _supervisor.LogReceived += OnLogReceived;
        _supervisor.StatusChanged += OnStatusChanged;

        _uptimeTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, RefreshUptimes, _dispatcher);
        _uptimeTimer.Start();
    }

    public string RepositoryRoot { get; }
    public ObservableCollection<ServiceViewModel> Services { get; }
    public ObservableCollection<LogLine> LogLines { get; } = [];
    public ICollectionView LogsView { get; }
    public IAsyncRelayCommand StartAllCommand { get; }
    public IAsyncRelayCommand StopAllCommand { get; }
    public IAsyncRelayCommand RestartAllCommand { get; }
    public IRelayCommand ClearLogsCommand { get; }
    public IRelayCommand SaveLogsCommand { get; }
    public IRelayCommand OpenFrontendCommand { get; }

    public string LogFilter
    {
        get => _logFilter;
        set
        {
            if (SetProperty(ref _logFilter, value))
                LogsView.Refresh();
        }
    }

    public bool ShowApiLogs
    {
        get => _showApiLogs;
        set
        {
            if (SetProperty(ref _showApiLogs, value))
                LogsView.Refresh();
        }
    }

    public bool ShowWebLogs
    {
        get => _showWebLogs;
        set
        {
            if (SetProperty(ref _showWebLogs, value))
                LogsView.Refresh();
        }
    }

    public bool AutoScroll
    {
        get => _autoScroll;
        set => SetProperty(ref _autoScroll, value);
    }

    public Uri? FrontendSource
    {
        get => _frontendSource;
        private set => SetProperty(ref _frontendSource, value);
    }

    public async Task ShutdownAsync()
    {
        _uptimeTimer.Stop();
        await _supervisor.DisposeAsync();
    }

    public async ValueTask DisposeAsync() => await ShutdownAsync();

    private async Task StartAllAsync()
    {
        foreach (var service in Services)
        {
            await service.StartAsync();
            await WaitForStableStateAsync(service);
            if (service.State != ServiceState.Running)
                return;
        }
    }

    private async Task StopAllAsync()
    {
        foreach (var service in Services.Reverse())
            await service.StopAsync();
    }

    private async Task RestartAllAsync()
    {
        await StopAllAsync();
        await StartAllAsync();
    }

    private static async Task WaitForStableStateAsync(ServiceViewModel service)
    {
        while (service.State == ServiceState.Starting)
            await Task.Delay(250);
    }

    private void OnLogReceived(LogLine line)
    {
        _dispatcher.InvokeAsync(() =>
        {
            LogLines.Add(line);
            while (LogLines.Count > MaximumLogLines)
                LogLines.RemoveAt(0);
        });
    }

    private void OnStatusChanged(ServiceStatusChangedEventArgs status)
    {
        _dispatcher.InvokeAsync(() =>
        {
            var service = Services.First(candidate => candidate.Id.Equals(status.ServiceId, StringComparison.OrdinalIgnoreCase));
            service.ApplyStatus(status);

            if (service.Id == "web")
                FrontendSource = status.State == ServiceState.Running ? service.Definition.LocalUri : null;
        });
    }

    private bool FilterLogLine(object item)
    {
        if (item is not LogLine line)
            return false;

        if (line.ServiceId == "api" && !ShowApiLogs || line.ServiceId == "web" && !ShowWebLogs)
            return false;

        return string.IsNullOrWhiteSpace(LogFilter) ||
               line.Text.Contains(LogFilter, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshUptimes(object? sender, EventArgs args)
    {
        var now = DateTimeOffset.Now;
        foreach (var service in Services)
            service.RefreshUptime(now);
    }

    private void SaveLogs()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save launcher logs",
            Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"tax-expense-tracker-{DateTime.Now:yyyyMMdd-HHmmss}.log"
        };

        if (dialog.ShowDialog() != true)
            return;

        File.WriteAllLines(dialog.FileName, LogLines.Select(line =>
            $"{line.Timestamp:O} [{line.ServiceId}] [{line.Stream}] {line.Text}"));
    }

    private static void OpenFrontend()
    {
        Process.Start(new ProcessStartInfo("http://localhost:4200") { UseShellExecute = true });
    }
}