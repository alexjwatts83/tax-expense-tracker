using TaxExpenseTrackerDevLauncher.Models;
using TaxExpenseTrackerDevLauncher.Services;
using TaxExpenseTrackerDevLauncher.ViewModels;

namespace TaxExpenseTrackerDevLauncher.Tests;

public sealed class ServiceViewModelTests
{
    [Fact]
    public async Task ApplyStatus_UpdatesCommandsProcessAndUptimeAcrossValidTransitions()
    {
        var definition = CreateDefinition();
        await using var supervisor = new ProcessSupervisor([definition]);
        var viewModel = new ServiceViewModel(definition, supervisor);
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-2);

        Assert.True(viewModel.StartCommand.CanExecute(null));
        Assert.False(viewModel.StopCommand.CanExecute(null));
        Assert.False(viewModel.RestartCommand.CanExecute(null));

        viewModel.ApplyStatus(new ServiceStatusChangedEventArgs("test", ServiceState.Starting, 123, startedAt, null));

        Assert.False(viewModel.StartCommand.CanExecute(null));
        Assert.True(viewModel.StopCommand.CanExecute(null));
        Assert.Equal(123, viewModel.ProcessId);

        viewModel.ApplyStatus(new ServiceStatusChangedEventArgs("test", ServiceState.Running, 123, startedAt, null));
        viewModel.RefreshUptime(startedAt.AddMinutes(2).AddSeconds(3));

        Assert.True(viewModel.RestartCommand.CanExecute(null));
        Assert.Equal("02:03", viewModel.Uptime);

        viewModel.ApplyStatus(new ServiceStatusChangedEventArgs("test", ServiceState.Stopped, null, null, 0));

        Assert.True(viewModel.StartCommand.CanExecute(null));
        Assert.False(viewModel.StopCommand.CanExecute(null));
        Assert.Null(viewModel.ProcessId);
        Assert.Equal("--", viewModel.Uptime);
        Assert.Equal(0, viewModel.ExitCode);
    }

    [Fact]
    public async Task ApplyStatus_CrashedAllowsStartAndRestart()
    {
        var definition = CreateDefinition();
        await using var supervisor = new ProcessSupervisor([definition]);
        var viewModel = new ServiceViewModel(definition, supervisor);

        viewModel.ApplyStatus(new ServiceStatusChangedEventArgs("test", ServiceState.Crashed, 456, DateTimeOffset.UtcNow, 7));

        Assert.True(viewModel.StartCommand.CanExecute(null));
        Assert.False(viewModel.StopCommand.CanExecute(null));
        Assert.True(viewModel.RestartCommand.CanExecute(null));
        Assert.Equal(7, viewModel.ExitCode);
        Assert.Equal("--", viewModel.Uptime);
    }

    private static ServiceDefinition CreateDefinition() => new(
        "test",
        "Test",
        "unused.exe",
        [],
        Environment.CurrentDirectory,
        [],
        "ready",
        new Uri("http://localhost"));
}