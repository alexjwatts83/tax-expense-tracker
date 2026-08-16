using System.Net;
using System.Net.Sockets;
using TaxExpenseTrackerDevLauncher.Models;
using TaxExpenseTrackerDevLauncher.Services;

namespace TaxExpenseTrackerDevLauncher.Tests;

public sealed class ProcessSupervisorIntegrationTests
{
    [Fact]
    public async Task StartAndStop_TransitionsThroughReadinessAndReleasesPort()
    {
        var port = ReserveAvailablePort();
        var script = $"$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, {port}); " +
                     "$listener.Start(); Write-Output 'LAUNCHER_TEST_READY'; " +
                     "while ($true) { Start-Sleep -Milliseconds 100 }";
        var definition = new ServiceDefinition(
            "test",
            "Test service",
            "powershell.exe",
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script],
            Environment.CurrentDirectory,
            [port],
            "LAUNCHER_TEST_READY",
            new Uri($"http://localhost:{port}"));
        await using var supervisor = new ProcessSupervisor([definition]);
        var statuses = new List<ServiceStatusChangedEventArgs>();
        var running = new TaskCompletionSource<ServiceStatusChangedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource<ServiceStatusChangedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        supervisor.StatusChanged += status =>
        {
            lock (statuses)
                statuses.Add(status);

            if (status.State == ServiceState.Running)
                running.TrySetResult(status);
            if (status.State == ServiceState.Stopped)
                stopped.TrySetResult(status);
        };

        await supervisor.StartAsync("test");
        var runningStatus = await running.Task.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.NotNull(runningStatus.ProcessId);
        Assert.Contains(PortInspector.GetOwners([port]), owner => owner.ProcessId == runningStatus.ProcessId);

        await supervisor.StopAsync("test");
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(10));

        ServiceState[] stateSequence;
        lock (statuses)
            stateSequence = statuses.Select(status => status.State).ToArray();

        Assert.Contains(ServiceState.Starting, stateSequence);
        Assert.Contains(ServiceState.Running, stateSequence);
        Assert.Contains(ServiceState.Stopping, stateSequence);
        Assert.Equal(ServiceState.Stopped, stateSequence[^1]);
        Assert.Empty(PortInspector.GetOwners([port]));
    }

    private static int ReserveAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}