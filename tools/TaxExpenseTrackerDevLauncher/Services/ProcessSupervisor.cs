using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using TaxExpenseTrackerDevLauncher.Models;

namespace TaxExpenseTrackerDevLauncher.Services;

public sealed class ProcessSupervisor : IAsyncDisposable
{
    private readonly Dictionary<string, ServiceRuntime> _services;
    private readonly HttpClient _healthClient;
    private bool _isDisposing;

    public ProcessSupervisor(IEnumerable<ServiceDefinition> definitions, LauncherSettings? settings = null,
        HttpMessageHandler? healthHandler = null)
    {
        settings ??= new LauncherSettings();
        settings.Validate();
        StartupTimeout = TimeSpan.FromSeconds(settings.StartupTimeoutSeconds);
        GracefulStopTimeout = TimeSpan.FromSeconds(settings.GracefulStopTimeoutSeconds);
        _services = definitions.ToDictionary(
            definition => definition.Id,
            definition => new ServiceRuntime(definition),
            StringComparer.OrdinalIgnoreCase);
            _healthClient = new HttpClient(healthHandler ?? new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
    }

            public TimeSpan StartupTimeout { get; }
            public TimeSpan GracefulStopTimeout { get; }

    public event Action<LogLine>? LogReceived;
    public event Action<ServiceStatusChangedEventArgs>? StatusChanged;
    public event Action<string, IReadOnlyList<PortOwner>>? PortConflictDetected;

    public async Task StartAsync(string serviceId)
    {
        var runtime = GetRuntime(serviceId);
        await runtime.OperationGate.WaitAsync();

        try
        {
            if (runtime.Process is { HasExited: false } || runtime.State is ServiceState.Starting or ServiceState.Running)
                return;

            var portOwners = PortInspector.GetOwners(runtime.Definition.Ports);
            if (portOwners.Count > 0)
            {
                foreach (var owner in portOwners)
                    Emit(runtime.Definition.Id, "stderr", $"Port {owner.Port} is already used by {owner.ProcessName} (PID {owner.ProcessId}).");

                PortConflictDetected?.Invoke(runtime.Definition.Id, portOwners);
                SetStatus(runtime, ServiceState.Stopped);
                return;
            }

            runtime.IntentionalStop = false;
            runtime.ReadyLogSeen = false;
            runtime.ReadinessCancellation?.Dispose();
            runtime.ReadinessCancellation = new CancellationTokenSource();
            SetStatus(runtime, ServiceState.Starting);

            var startInfo = new ProcessStartInfo
            {
                FileName = ExecutableResolver.Resolve(runtime.Definition.Executable),
                WorkingDirectory = runtime.Definition.WorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            foreach (var argument in runtime.Definition.Arguments)
                startInfo.ArgumentList.Add(argument);

            startInfo.Environment["NO_COLOR"] = "1";

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (_, args) => ReceiveOutput(runtime, "stdout", args.Data);
            process.ErrorDataReceived += (_, args) => ReceiveOutput(runtime, "stderr", args.Data);
            process.Exited += (_, _) => HandleExit(runtime, process);

            if (!process.Start())
                throw new InvalidOperationException($"The {runtime.Definition.DisplayName} process did not start.");

            runtime.Process = process;
            runtime.StartedAt = DateTimeOffset.Now;
            SetStatus(runtime, ServiceState.Starting, process.Id, runtime.StartedAt);
            Emit(runtime.Definition.Id, "launcher", $"Started process {process.Id}: {startInfo.FileName} {string.Join(' ', runtime.Definition.Arguments)}");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _ = MonitorReadinessAsync(runtime, process, runtime.ReadinessCancellation.Token);
        }
        catch (Exception exception)
        {
            Emit(runtime.Definition.Id, "stderr", $"Start failed: {exception.Message}");
            SetStatus(runtime, ServiceState.Crashed);
        }
        finally
        {
            runtime.OperationGate.Release();
        }
    }

    public async Task StopAsync(string serviceId)
    {
        var runtime = GetRuntime(serviceId);
        await runtime.OperationGate.WaitAsync();

        try
        {
            var process = runtime.Process;
            if (process is null || process.HasExited)
            {
                SetStatus(runtime, ServiceState.Stopped);
                return;
            }

            runtime.IntentionalStop = true;
            runtime.ReadinessCancellation?.Cancel();
            SetStatus(runtime, ServiceState.Stopping, process.Id, runtime.StartedAt);
            Emit(runtime.Definition.Id, "launcher", $"Stopping process tree {process.Id}...");

            if (process.CloseMainWindow())
            {
                using var gracefulTimeout = new CancellationTokenSource(GracefulStopTimeout);
                try
                {
                    await process.WaitForExitAsync(gracefulTimeout.Token);
                }
                catch (OperationCanceledException)
                {
                }
            }

            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            SetStatus(runtime, ServiceState.Stopped);
        }
        catch (Exception exception)
        {
            Emit(runtime.Definition.Id, "stderr", $"Stop failed: {exception.Message}");
            SetStatus(runtime, ServiceState.Crashed, exitCode: TryGetExitCode(runtime.Process));
        }
        finally
        {
            runtime.OperationGate.Release();
        }
    }

    public async Task RestartAsync(string serviceId)
    {
        await StopAsync(serviceId);
        await StartAsync(serviceId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposing)
            return;

        _isDisposing = true;

        foreach (var runtime in _services.Values.Reverse())
            await StopAsync(runtime.Definition.Id);

        foreach (var runtime in _services.Values)
        {
            runtime.ReadinessCancellation?.Dispose();
            runtime.Process?.Dispose();
            runtime.OperationGate.Dispose();
        }

        _healthClient.Dispose();
    }

    private async Task MonitorReadinessAsync(ServiceRuntime runtime, Process process, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + StartupTimeout;

        try
        {
            while (DateTimeOffset.UtcNow < deadline && !cancellationToken.IsCancellationRequested && !process.HasExited)
            {
                if (runtime.ReadyLogSeen && ArePortsListening(runtime.Definition.Ports))
                {
                    if (runtime.Definition.HealthUri is { } healthUri)
                    {
                        using var healthTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        var remaining = deadline - DateTimeOffset.UtcNow;
                        if (remaining <= TimeSpan.Zero)
                            break;

                        healthTimeout.CancelAfter(remaining);
                        try
                        {
                            using var response = await _healthClient.GetAsync(healthUri,
                                HttpCompletionOption.ResponseHeadersRead, healthTimeout.Token);
                            if (cancellationToken.IsCancellationRequested || process.HasExited)
                                return;

                            if (response.StatusCode != HttpStatusCode.OK)
                            {
                                SetStatus(runtime, ServiceState.Unhealthy, process.Id, runtime.StartedAt);
                                Emit(runtime.Definition.Id, "stderr", $"Health check {healthUri} returned HTTP {(int)response.StatusCode}. Check API/database configuration and logs, then restart.");
                                return;
                            }
                        }
                        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                        catch (HttpRequestException exception) when (exception.HttpRequestError == HttpRequestError.SecureConnectionError)
                        {
                            if (cancellationToken.IsCancellationRequested || process.HasExited)
                                return;

                            SetStatus(runtime, ServiceState.CertificateError, process.Id, runtime.StartedAt);
                            Emit(runtime.Definition.Id, "stderr", $"Health check {healthUri}: HTTPS certificate validation or TLS negotiation failed. Run 'dotnet dev-certs https --check --trust', check the certificate hostname/expiry, then restart. {exception.Message}");
                            return;
                        }
                        catch (HttpRequestException exception)
                        {
                            Emit(runtime.Definition.Id, "stderr", $"Health check {healthUri} could not connect: {exception.Message}");
                            await Task.Delay(400, cancellationToken);
                            continue;
                        }
                    }

                    if (cancellationToken.IsCancellationRequested || process.HasExited)
                        return;

                    SetStatus(runtime, ServiceState.Running, process.Id, runtime.StartedAt);
                    Emit(runtime.Definition.Id, "launcher", $"Ready on port(s) {string.Join(", ", runtime.Definition.Ports)}.");
                    return;
                }

                await Task.Delay(400, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested || process.HasExited)
                return;

            if (runtime.Definition.HealthUri is { } timedOutUri)
            {
                SetStatus(runtime, ServiceState.HealthTimeout, process.Id, runtime.StartedAt);
                Emit(runtime.Definition.Id, "stderr", $"API readiness timed out after {StartupTimeout.TotalSeconds:0} seconds waiting for startup output, listening ports, and HTTP 200 from {timedOutUri}. Check API logs/connectivity or increase startupTimeoutSeconds, then restart.");
                return;
            }

            Emit(runtime.Definition.Id, "stderr", $"Startup timed out after {StartupTimeout.TotalSeconds:0} seconds.");
            process.Kill(entireProcessTree: true);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Emit(runtime.Definition.Id, "stderr", $"Readiness check failed: {exception.Message}");
        }
    }

    private void ReceiveOutput(ServiceRuntime runtime, string stream, string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (text.Contains(runtime.Definition.ReadyLogPattern, StringComparison.OrdinalIgnoreCase))
            runtime.ReadyLogSeen = true;

        Emit(runtime.Definition.Id, stream, text);
    }

    private void HandleExit(ServiceRuntime runtime, Process process)
    {
        if (!ReferenceEquals(runtime.Process, process))
            return;

        var exitCode = TryGetExitCode(process);
        var finalState = runtime.IntentionalStop || _isDisposing
            ? ServiceState.Stopped
            : ServiceState.Crashed;

        Emit(runtime.Definition.Id, finalState == ServiceState.Crashed ? "stderr" : "launcher", $"Process exited with code {exitCode?.ToString() ?? "unknown"}.");
        SetStatus(runtime, finalState, exitCode: exitCode);
    }

    private void SetStatus(
        ServiceRuntime runtime,
        ServiceState state,
        int? processId = null,
        DateTimeOffset? startedAt = null,
        int? exitCode = null)
    {
        runtime.State = state;
        StatusChanged?.Invoke(new ServiceStatusChangedEventArgs(
            runtime.Definition.Id,
            state,
            processId,
            startedAt,
            exitCode));
    }

    private void Emit(string serviceId, string stream, string text) =>
        LogReceived?.Invoke(new LogLine(DateTimeOffset.Now, serviceId, stream, text));

    private ServiceRuntime GetRuntime(string serviceId)
    {
        if (_services.TryGetValue(serviceId, out var runtime))
            return runtime;

        throw new ArgumentException($"Unknown service id '{serviceId}'.", nameof(serviceId));
    }

    private static bool ArePortsListening(IReadOnlyList<int> ports)
    {
        var listeningPorts = IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Select(endpoint => endpoint.Port)
            .ToHashSet();

        return ports.All(listeningPorts.Contains);
    }

    private static int? TryGetExitCode(Process? process)
    {
        try
        {
            return process is { HasExited: true } ? process.ExitCode : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private sealed class ServiceRuntime(ServiceDefinition definition)
    {
        public ServiceDefinition Definition { get; } = definition;
        public SemaphoreSlim OperationGate { get; } = new(1, 1);
        public Process? Process { get; set; }
        public CancellationTokenSource? ReadinessCancellation { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public ServiceState State { get; set; } = ServiceState.Stopped;
        public bool ReadyLogSeen { get; set; }
        public bool IntentionalStop { get; set; }
    }
}