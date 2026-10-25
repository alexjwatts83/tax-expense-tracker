using TaxExpenseTrackerDevLauncher.Services;

namespace TaxExpenseTrackerDevLauncher.Tests;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public async Task DuplicateInstance_NotifiesPrimaryWithoutBecomingPrimary()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        using var primary = new SingleInstanceCoordinator(root);
        var activation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        primary.ActivationRequested += () => activation.TrySetResult();
        using var duplicate = new SingleInstanceCoordinator(root.ToUpperInvariant() + Path.DirectorySeparatorChar);

        Assert.True(primary.IsPrimary);
        Assert.False(duplicate.IsPrimary);
        duplicate.RequestActivation();

        await activation.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void DifferentRepositories_HaveIndependentPrimaryInstances()
    {
        using var first = new SingleInstanceCoordinator(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        using var second = new SingleInstanceCoordinator(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        Assert.True(first.IsPrimary);
        Assert.True(second.IsPrimary);
    }

    [Fact]
    public void DisposedPrimary_AllowsNewPrimary()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        using (var original = new SingleInstanceCoordinator(root))
            Assert.True(original.IsPrimary);

        using var replacement = new SingleInstanceCoordinator(root);
        Assert.True(replacement.IsPrimary);
    }
}