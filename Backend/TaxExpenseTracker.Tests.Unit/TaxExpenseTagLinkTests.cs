using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class TaxExpenseTagLinkTests
{
    private static TaxExpense NewExpense() =>
        TaxExpense.Create("Desc", new DateOnly(2026, 5, 1), Guid.NewGuid(), 10m, Guid.NewGuid(), TestTime.TimeProvider);

    [Fact]
    public void ReplaceTagLinks_PreservesExistingLinkIdentities()
    {
        var expense = NewExpense();
        var tagA = Guid.NewGuid();
        expense.ReplaceTagLinks([tagA]);
        var originalLinkId = expense.TaxExpenseTags.Single().Id;

        expense.ReplaceTagLinks([tagA, Guid.NewGuid()]);

        Assert.Equal(2, expense.TaxExpenseTags.Count);
        Assert.Equal(originalLinkId, expense.TaxExpenseTags.Single(x => x.TagId == tagA).Id);
    }

    [Fact]
    public void ReplaceTagLinks_RemovesDroppedTagsAndDeduplicates()
    {
        var expense = NewExpense();
        var tagA = Guid.NewGuid();
        var tagB = Guid.NewGuid();
        expense.ReplaceTagLinks([tagA, tagB]);

        expense.ReplaceTagLinks([tagB, tagB]);

        Assert.Single(expense.TaxExpenseTags);
        Assert.Equal(tagB, expense.TaxExpenseTags.Single().TagId);
    }

    [Fact]
    public void AddTagLink_IsIdempotentAndPreservesProvidedId()
    {
        var expense = NewExpense();
        var tagId = Guid.NewGuid();
        var linkId = Guid.NewGuid();

        expense.AddTagLink(tagId, linkId);
        expense.AddTagLink(tagId, Guid.NewGuid());

        var link = Assert.Single(expense.TaxExpenseTags);
        Assert.Equal(linkId, link.Id);
    }

    [Fact]
    public void RemoveTagLink_ReturnsFalse_WhenTagAbsent()
    {
        var expense = NewExpense();

        Assert.False(expense.RemoveTagLink(Guid.NewGuid()));
    }

    [Fact]
    public void Import_PreservesSuppliedId()
    {
        var id = Guid.NewGuid();

        var expense = TaxExpense.Import(id, "Desc", new DateOnly(2026, 5, 1), Guid.NewGuid(), 10m, Guid.NewGuid(), TestTime.TimeProvider);

        Assert.Equal(id, expense.Id);
    }
}
