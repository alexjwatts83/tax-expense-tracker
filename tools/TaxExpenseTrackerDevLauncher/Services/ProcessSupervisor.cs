using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using TaxExpenseTrackerDevLauncher.Models;

namespace TaxExpenseTrackerDevLauncher.Services;

public sealed class ProcessSupervisor : IAsyncDisposable
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan GracefulStopTimeout = TimeSpan.FromSeconds(2);
    private readonly Dictionary<string, ServiceRuntime> _services;
    private bool _isDisposing;

    public ProcessSupervisor(IEnumerable<ServiceDefinition> definitions)
    {
        _services = definitions.ToDictionary(
            definition => definition.Id,
            definition => new ServiceRuntime(definition),
            StringComparer.OrdinalIgnoreCase);
    }

    public event Action<LogLine>? LogReceived;
    public event Action<ServiceStatusChangedEventArgs>? StatusChanged;

    public async Task StartAsync(string serviceId)
    {
        var runtime = GetRuntime(serviceId);
        await runtime.OperationGate.WaitAsync();

        try
        {
            if (runtime.State is ServiceState.Starting or ServiceState.Running)
                return;

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
                    SetStatus(runtime, ServiceState.Running, process.Id, runtime.StartedAt);
                    Emit(runtime.Definition.Id, "launcher", $"Ready on port(s) {string.Join(", ", runtime.Definition.Ports)}.");
                    return;
                }

                await Task.Delay(400, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested || process.HasExited)
                return;

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