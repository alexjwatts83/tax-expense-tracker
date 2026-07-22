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

        Assert.Equal(string.Empty, expense.Item);
        Assert.Equal(string.Empty, expense.Description);
        Assert.Equal(string.Empty, expense.Bank);
        Assert.NotNull(expense.TaxExpenseTags);
        Assert.Empty(expense.TaxExpenseTags);
    }
}
