using TaxExpenseTracker.Application.Expenses;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class ExpenseServiceTests
{
    [Fact]
    public async Task CreateAsync_Throws_WhenSourceMissing()
    {
        var repository = new InMemoryExpenseRepository
        {
            SourceExistsResult = false
        };

        var service = new ExpenseService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateExpenseCommand(
                "Laptop",
                "Work machine",
                DateTime.UtcNow,
                "ANZ",
                1200m,
                Guid.NewGuid(),
                [])));
    }

    [Fact]
    public async Task CreateAsync_PersistsExpense_WhenSourceExists()
    {
        var repository = new InMemoryExpenseRepository
        {
            SourceExistsResult = true
        };

        var service = new ExpenseService(repository);

        var result = await service.CreateAsync(new CreateExpenseCommand(
            "  Laptop  ",
            "  Work machine  ",
            DateTime.UtcNow,
            "  ANZ  ",
            1200m,
            repository.SourceId,
            [repository.TagId]));

        Assert.Equal("Laptop", result.Item);
        Assert.Equal("ANZ", result.Bank);
        Assert.Single(repository.Expenses);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenExpenseMissing()
    {
        var repository = new InMemoryExpenseRepository
        {
            SourceExistsResult = true
        };

        var service = new ExpenseService(repository);

        var result = await service.UpdateAsync(Guid.NewGuid(), new UpdateExpenseCommand(
            "Item",
            "Desc",
            DateTime.UtcNow,
            "Bank",
            10m,
            repository.SourceId,
            [repository.TagId]));

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenExpenseMissing()
    {
        var repository = new InMemoryExpenseRepository();
        var service = new ExpenseService(repository);

        var result = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task RestoreAsync_RestoresExpense_WhenSoftDeleted()
    {
        var repository = new InMemoryExpenseRepository
        {
            SourceExistsResult = true
        };

        var expense = TaxExpense.Create(
            "Laptop",
            "Desc",
            DateTime.UtcNow,
            "ANZ",
            100m,
            repository.SourceId);
        expense.SoftDelete();
        repository.Expenses.Add(expense);

        var service = new ExpenseService(repository);

        var result = await service.RestoreAsync(expense.Id);

        Assert.True(result);
        Assert.False(expense.IsDeleted);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsGroupedTotals()
    {
        var repository = new InMemoryExpenseRepository { SourceExistsResult = true };
        var now = DateTime.UtcNow;

        var first = TaxExpense.Create("Item A", "Desc", now, "ANZ", 10m, repository.SourceId);
        first.Source = Tracker.Create("Tracker A", "Source", now);

        var second = TaxExpense.Create("Item B", "Desc", now, "CBA", 30m, repository.SourceId);
        second.Source = Tracker.Create("Tracker B", "Source", now);

        repository.Expenses.Add(first);
        repository.Expenses.Add(second);

        var service = new ExpenseService(repository);
        var summary = await service.GetSummaryAsync();

        Assert.Equal(40m, summary.TotalSpent);
        Assert.Equal(2, summary.ByBank.Count);
        Assert.Equal(2, summary.BySource.Count);
    }

    [Fact]
    public async Task FilterAsync_Throws_WhenDateRangeInvalid()
    {
        var repository = new InMemoryExpenseRepository();
        var service = new ExpenseService(repository);

        var query = new ExpenseFilterQuery(
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1),
            null,
            null,
            null,
            []);

        await Assert.ThrowsAsync<ArgumentException>(() => service.FilterAsync(query));
    }

    private sealed class InMemoryExpenseRepository : IExpenseRepository
    {
        public List<TaxExpense> Expenses { get; } = [];
        public bool SaveChangesCalled { get; private set; }
        public bool SourceExistsResult { get; set; }
        public Guid SourceId { get; } = Guid.NewGuid();
        public Guid TagId { get; } = Guid.NewGuid();

        public Task<IReadOnlyList<TaxExpense>> GetPagedWithDetailsAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TaxExpense>>(Expenses.Skip(skip).Take(take).ToList());
        }

        public Task<TaxExpense?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var expense = Expenses.FirstOrDefault(x => x.Id == id);
            if (expense is not null && expense.Source is null)
            {
                expense.Source = Tracker.Create("Home Office", "Source", DateTime.UtcNow);
            }

            if (expense is not null)
            {
                foreach (var tag in expense.TaxExpenseTags.Where(x => x.Tag is null))
                {
                    tag.Tag = Tag.Create("Deductible", DateTime.UtcNow);
                }
            }

            return Task.FromResult(expense);
        }

        public Task<TaxExpense?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Expenses.FirstOrDefault(x => x.Id == id));
        }

        public Task<TaxExpense?> GetByIdForRestoreAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Expenses.FirstOrDefault(x => x.Id == id));
        }

        public Task<bool> SourceExistsAsync(Guid sourceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SourceExistsResult && sourceId == SourceId);
        }

        public Task<IReadOnlyList<Guid>> GetExistingTagIdsAsync(IReadOnlyList<Guid> tagIds, CancellationToken cancellationToken = default)
        {
            var valid = tagIds.Where(x => x == TagId).ToList();
            return Task.FromResult<IReadOnlyList<Guid>>(valid);
        }

        public Task AddAsync(TaxExpense expense, CancellationToken cancellationToken = default)
        {
            Expenses.Add(expense);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }

        public Task<decimal> GetTotalSpentAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Expenses.Sum(x => x.Price));
        }

        public Task<IReadOnlyList<ExpenseTotalByGroup>> GetTotalByBankAsync(CancellationToken cancellationToken = default)
        {
            var rows = Expenses
                .GroupBy(x => x.Bank)
                .Select(x => new ExpenseTotalByGroup(x.Key, x.Sum(y => y.Price)))
                .ToList();

            return Task.FromResult<IReadOnlyList<ExpenseTotalByGroup>>(rows);
        }

        public Task<IReadOnlyList<ExpenseTotalByGroup>> GetTotalBySourceAsync(CancellationToken cancellationToken = default)
        {
            var rows = Expenses
                .GroupBy(x => x.Source?.Name ?? "Unknown")
                .Select(x => new ExpenseTotalByGroup(x.Key, x.Sum(y => y.Price)))
                .ToList();

            return Task.FromResult<IReadOnlyList<ExpenseTotalByGroup>>(rows);
        }

        public Task<IReadOnlyList<TaxExpense>> FilterWithDetailsAsync(ExpenseFilterQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TaxExpense>>(Expenses.ToList());
        }
    }
}
