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
    private readonly ApiLogFileReader _apiLogFileReader;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _uptimeTimer;
    private string _logFilter = string.Empty;
    private string _frontendOutputFilter = string.Empty;
    private bool _showApiLogs = true;
    private bool _showWebLogs = true;
    private bool _autoScroll = true;
    private bool _frontendOutputAutoScroll = true;
    private string _apiLogFileFilter = string.Empty;
    private bool _apiLogFileAutoScroll = true;
    private ApiLogFileInfo? _selectedApiLogFile;
    private string _apiLogFileStatus = "Loading API log files...";
    private Uri? _frontendSource;
    private bool _isFrontendRunning;
    private string _frontendBrowserStatus = "Start the Web service to load the site.";

    public MainViewModel()
    {
        _dispatcher = Application.Current.Dispatcher;
        RepositoryRoot = RepositoryLocator.FindRepositoryRoot();
        var definitions = ServiceRegistry.Create(RepositoryRoot);
        _supervisor = new ProcessSupervisor(definitions);
        _apiLogFileReader = new ApiLogFileReader();
        Services = new ObservableCollection<ServiceViewModel>(
            definitions.Select(definition => new ServiceViewModel(definition, _supervisor)));

        LogsView = CollectionViewSource.GetDefaultView(LogLines);
        LogsView.Filter = FilterLogLine;
        FrontendLogsView = new ListCollectionView(LogLines)
        {
            Filter = FilterFrontendLogLine
        };
        ApiLogFileLinesView = CollectionViewSource.GetDefaultView(ApiLogFileLines);
        ApiLogFileLinesView.Filter = FilterApiLogFileLine;

        StartAllCommand = new AsyncRelayCommand(StartAllAsync);
        StopAllCommand = new AsyncRelayCommand(StopAllAsync);
        RestartAllCommand = new AsyncRelayCommand(RestartAllAsync);
        ClearLogsCommand = new RelayCommand(LogLines.Clear);
        SaveLogsCommand = new RelayCommand(SaveLogs);
        OpenFrontendCommand = new RelayCommand(OpenFrontend);
        RefreshApiLogFilesCommand = new AsyncRelayCommand(_apiLogFileReader.RefreshFilesAsync);
        OpenApiLogFolderCommand = new RelayCommand(OpenApiLogFolder);

        _supervisor.LogReceived += OnLogReceived;
        _supervisor.StatusChanged += OnStatusChanged;
        _supervisor.PortConflictDetected += OnPortConflictDetected;
        _apiLogFileReader.FilesChanged += OnApiLogFilesChanged;
        _apiLogFileReader.LinesReset += OnApiLogLinesReset;
        _apiLogFileReader.LinesAppended += OnApiLogLinesAppended;
        _apiLogFileReader.StatusChanged += status => _dispatcher.InvokeAsync(() => ApiLogFileStatus = status);

        _uptimeTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, RefreshUptimes, _dispatcher);
        _uptimeTimer.Start();
        _ = _apiLogFileReader.InitializeAsync();
    }

    public string RepositoryRoot { get; }
    public ObservableCollection<ServiceViewModel> Services { get; }
    public ServiceViewModel WebService => Services.First(service => service.Id == "web");
    public ObservableCollection<LogLine> LogLines { get; } = [];
    public ICollectionView LogsView { get; }
    public ICollectionView FrontendLogsView { get; }
    public ObservableCollection<ApiLogFileInfo> ApiLogFiles { get; } = [];
    public ObservableCollection<ApiLogFileLine> ApiLogFileLines { get; } = [];
    public ICollectionView ApiLogFileLinesView { get; }
    public IAsyncRelayCommand StartAllCommand { get; }
    public IAsyncRelayCommand StopAllCommand { get; }
    public IAsyncRelayCommand RestartAllCommand { get; }
    public IRelayCommand ClearLogsCommand { get; }
    public IRelayCommand SaveLogsCommand { get; }
    public IRelayCommand OpenFrontendCommand { get; }
    public IAsyncRelayCommand RefreshApiLogFilesCommand { get; }
    public IRelayCommand OpenApiLogFolderCommand { get; }

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

    public string FrontendOutputFilter
    {
        get => _frontendOutputFilter;
        set
        {
            if (SetProperty(ref _frontendOutputFilter, value))
                FrontendLogsView.Refresh();
        }
    }

    public bool FrontendOutputAutoScroll
    {
        get => _frontendOutputAutoScroll;
        set => SetProperty(ref _frontendOutputAutoScroll, value);
    }

    public string ApiLogFileFilter
    {
        get => _apiLogFileFilter;
        set
        {
            if (SetProperty(ref _apiLogFileFilter, value))
                ApiLogFileLinesView.Refresh();
        }
    }

    public bool ApiLogFileAutoScroll
    {
        get => _apiLogFileAutoScroll;
        set => SetProperty(ref _apiLogFileAutoScroll, value);
    }

    public ApiLogFileInfo? SelectedApiLogFile
    {
        get => _selectedApiLogFile;
        set
        {
            if (SetProperty(ref _selectedApiLogFile, value))
                _ = _apiLogFileReader.SelectFileAsync(value?.FullPath);
        }
    }

    public string ApiLogFileStatus
    {
        get => _apiLogFileStatus;
        private set => SetProperty(ref _apiLogFileStatus, value);
    }

    public Uri? FrontendSource
    {
        get => _frontendSource;
        private set => SetProperty(ref _frontendSource, value);
    }

    public bool IsFrontendRunning
    {
        get => _isFrontendRunning;
        private set
        {
            if (SetProperty(ref _isFrontendRunning, value))
                OnPropertyChanged(nameof(IsFrontendUnavailable));
        }
    }

    public bool IsFrontendUnavailable => !IsFrontendRunning;

    public string FrontendBrowserStatus
    {
        get => _frontendBrowserStatus;
        private set => SetProperty(ref _frontendBrowserStatus, value);
    }

    public async Task ShutdownAsync()
    {
        _uptimeTimer.Stop();
        await _apiLogFileReader.DisposeAsync();
        await _supervisor.DisposeAsync();
    }

    public async ValueTask DisposeAsync() => await ShutdownAsync();

    public void ReportBrowserStatus(string message, bool isError = false)
    {
        FrontendBrowserStatus = message;
        if (isError)
            OnLogReceived(new LogLine(DateTimeOffset.Now, "web", "stderr", message));
    }

    private async Task StartAllAsync()
    {
        foreach (var service in Services)
            await service.StartAsync();
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
            {
                IsFrontendRunning = status.State == ServiceState.Running;
                FrontendSource = IsFrontendRunning ? service.Definition.LocalUri : null;
                FrontendBrowserStatus = status.State switch
                {
                    ServiceState.Starting => "Web service is starting...",
                    ServiceState.Running => "Loading the frontend...",
                    ServiceState.Stopping => "Web service is stopping...",
                    ServiceState.Crashed => "Web service exited unexpectedly. Check Frontend Output.",
                    _ => "Start the Web service to load the site."
                };
            }
        });
    }

    private void OnPortConflictDetected(string serviceId, IReadOnlyList<PortOwner> owners)
    {
        _dispatcher.InvokeAsync(() =>
        {
            var service = Services.First(candidate => candidate.Id.Equals(serviceId, StringComparison.OrdinalIgnoreCase));
            service.ApplyPortConflicts(owners);
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

    private bool FilterFrontendLogLine(object item)
    {
        if (item is not LogLine line || !line.ServiceId.Equals("web", StringComparison.OrdinalIgnoreCase))
            return false;

        return string.IsNullOrWhiteSpace(FrontendOutputFilter) ||
               line.Text.Contains(FrontendOutputFilter, StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterApiLogFileLine(object item) =>
        item is ApiLogFileLine line &&
        (string.IsNullOrWhiteSpace(ApiLogFileFilter) ||
         line.Text.Contains(ApiLogFileFilter, StringComparison.OrdinalIgnoreCase));

    private void OnApiLogFilesChanged(IReadOnlyList<ApiLogFileInfo> files)
    {
        _dispatcher.InvokeAsync(() =>
        {
            var selectedPath = SelectedApiLogFile?.FullPath;
            ApiLogFiles.Clear();
            foreach (var file in files)
                ApiLogFiles.Add(file);

            SelectedApiLogFile = ApiLogFiles.FirstOrDefault(file =>
                string.Equals(file.FullPath, selectedPath, StringComparison.OrdinalIgnoreCase)) ?? ApiLogFiles.FirstOrDefault();
        });
    }

    private void OnApiLogLinesReset(IReadOnlyList<string> lines)
    {
        _dispatcher.InvokeAsync(() =>
        {
            ApiLogFileLines.Clear();
            AppendApiLogLines(lines);
        });
    }

    private void OnApiLogLinesAppended(IReadOnlyList<string> lines) =>
        _dispatcher.InvokeAsync(() => AppendApiLogLines(lines));

    private void AppendApiLogLines(IEnumerable<string> lines)
    {
        foreach (var line in lines)
            ApiLogFileLines.Add(new ApiLogFileLine(line));

        while (ApiLogFileLines.Count > MaximumLogLines)
            ApiLogFileLines.RemoveAt(0);
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

    private static void OpenApiLogFolder()
    {
        if (!Directory.Exists(ApiLogFileReader.DefaultLogDirectory))
            return;

        Process.Start(new ProcessStartInfo("explorer.exe", ApiLogFileReader.DefaultLogDirectory) { UseShellExecute = true });
    }
}