using Microsoft.Extensions.Time.Testing;

namespace TaxExpenseTracker.Tests.Unit;

internal static class TestTime
{
    public static readonly DateTimeOffset FixedUtcNow = new(2026, 7, 23, 9, 0, 0, TimeSpan.Zero);
    public static readonly FakeTimeProvider TimeProvider = new(FixedUtcNow);
}