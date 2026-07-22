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
        Assert.Throws<ArgumentException>(() => Tracker.Create("   ", null));
    }

    [Fact]
    public void Tag_Create_Throws_WhenNameMissing()
    {
        Assert.Throws<ArgumentException>(() => Tag.Create(""));
    }

    [Fact]
    public void TaxExpense_Create_Throws_WhenPriceNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TaxExpense.Create("Desc", DateTime.UtcNow, Guid.NewGuid(), -1m, Guid.NewGuid()));
    }

    [Fact]
    public void TaxExpense_Create_Throws_WhenDateDefault()
    {
        Assert.Throws<ArgumentException>(() =>
            TaxExpense.Create("Desc", default, Guid.NewGuid(), 1m, Guid.NewGuid()));
    }
}
