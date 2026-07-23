using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class DomainEntityDefaultsTests
{
    [Fact]
    public void Tracker_Defaults_AreInitialized()
    {
        var tracker = new Tracker();

        Assert.Equal(string.Empty, tracker.Name);
        Assert.NotNull(tracker.Expenses);
        Assert.Empty(tracker.Expenses);
    }

    [Fact]
    public void TaxExpense_Defaults_AreInitialized()
    {
        var expense = new TaxExpense();

        Assert.Equal(string.Empty, expense.Description);
        Assert.Equal(Guid.Empty, expense.BankId);
        Assert.NotNull(expense.TaxExpenseTags);
        Assert.Empty(expense.TaxExpenseTags);
    }

    [Fact]
    public void Tracker_Create_Throws_WhenNameMissing()
    {
        Assert.Throws<ArgumentException>(() => Tracker.Create("   ", null, TestTime.TimeProvider));
    }

    [Fact]
    public void Tag_Create_Throws_WhenNameMissing()
    {
        Assert.Throws<ArgumentException>(() => Tag.Create("", TestTime.TimeProvider));
    }

    [Fact]
    public void TaxExpense_Create_Throws_WhenPriceNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TaxExpense.Create("Desc", TestTime.FixedUtcNow.UtcDateTime, Guid.NewGuid(), -1m, Guid.NewGuid(), TestTime.TimeProvider));
    }

    [Fact]
    public void TaxExpense_Create_Throws_WhenDateDefault()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TaxExpense.Create("Desc", default, Guid.NewGuid(), 1m, Guid.NewGuid(), TestTime.TimeProvider));
    }

    [Fact]
    public void LeaveEntry_Create_Defaults_ToAnnualLeaveType()
    {
        var entry = LeaveEntry.Create(new DateTime(2026, 3, 10), DayEntryType.FullDay, null, null, TestTime.TimeProvider);

        Assert.Equal(LeaveType.Annual, entry.LeaveType);
    }

    [Fact]
    public void LeaveEntry_Create_Throws_WhenLeaveTypeInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LeaveEntry.Create(new DateTime(2026, 3, 10), DayEntryType.FullDay, null, null, TestTime.TimeProvider, (LeaveType)99));
    }
}
