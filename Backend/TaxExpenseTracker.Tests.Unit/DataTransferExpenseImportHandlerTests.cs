using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.DataTransfer;
using TaxExpenseTracker.Application.Expenses;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class DataTransferExpenseImportHandlerTests
{
    [Fact]
    public async Task ImportAsync_DryRun_ResolvesTagForExpenseInSamePayload()
    {
        var repository = new DataTransferExpenseRepository();
        var handler = new DataTransferExpenseImportHandler(repository, TestTime.TimeProvider);
        var expenseId = Guid.NewGuid();

        var results = await handler.ImportAsync(
            new ExpenseImportPayloadDto(
                [new ExpenseImportItemDto(
                    expenseId,
                    TestTime.FixedUtcNow.UtcDateTime,
                    "Laptop",
                    1200m,
                    repository.BankId,
                    repository.SourceId,
                    null,
                    null,
                    false)],
                [new ExpenseTagImportItemDto(Guid.NewGuid(), expenseId, repository.TagId)]),
            new DataTransferImportOptions(DryRun: true));

        var expenseResult = Assert.Single(results, x => x.Entity == "expenses");
        var tagResult = Assert.Single(results, x => x.Entity == "expenseTags");

        Assert.Equal(1, expenseResult.CreatedCount);
        Assert.Empty(expenseResult.Errors);
        Assert.Equal(1, tagResult.CreatedCount);
        Assert.Empty(tagResult.Errors);
        Assert.Empty(repository.Expenses);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ImportAsync_ReplaceWithDeletes_SoftDeletesExpenseMissingFromPayload()
    {
        var repository = new DataTransferExpenseRepository();
        var kept = TaxExpense.Create("Kept", new DateTime(2026, 7, 1), repository.BankId, 10m, repository.SourceId, TestTime.TimeProvider);
        var missing = TaxExpense.Create("Missing", new DateTime(2026, 7, 2), repository.BankId, 20m, repository.SourceId, TestTime.TimeProvider);
        kept.TaxExpenseTags.Add(TaxExpenseTag.Create(kept.Id, repository.TagId));
        repository.Expenses.AddRange([kept, missing]);
        var handler = new DataTransferExpenseImportHandler(repository, TestTime.TimeProvider);

        var results = await handler.ImportAsync(
            new ExpenseImportPayloadDto(
                [new ExpenseImportItemDto(kept.Id, kept.Date, kept.Description, kept.Price, kept.BankId, kept.SourceId, null, null, false)],
                []),
            new DataTransferImportOptions(DataTransferImportMode.Replace, AllowDeletes: true));

        var expenseResult = Assert.Single(results, x => x.Entity == "expenses");
        Assert.False(kept.IsDeleted);
        Assert.True(missing.IsDeleted);
        Assert.Empty(kept.TaxExpenseTags);
        Assert.True(repository.SaveChangesCalled);
        Assert.Contains(expenseResult.Warnings, x => x.Code == "WARN_REPLACE_SOFT_DELETED_MISSING");
        var tagResult = Assert.Single(results, x => x.Entity == "expenseTags");
        Assert.Contains(tagResult.Warnings, x => x.Code == "WARN_REPLACE_DELETED_MISSING");
    }

    [Fact]
    public void BuildEntityResult_PreservesTypedIssueCodes()
    {
        var factory = new DataTransferImportResultFactory();
        var computation = new DataTransferEntityImportComputation(
            "expenses",
            1,
            0,
            0,
            0,
            [new DataTransferImportIssue("WARN_TEST", "warning")],
            [new DataTransferImportIssue("ERR_TEST", "error")]);

        var result = factory.BuildEntityResult(computation);

        Assert.Equal(["WARN_TEST"], result.WarningCodes);
        Assert.Equal(["warning"], result.Warnings);
        Assert.Equal(["ERR_TEST"], result.ErrorCodes);
        Assert.Equal(["error"], result.Errors);
    }

    private sealed class DataTransferExpenseRepository : IExpenseRepository
    {
        public Guid BankId { get; } = Guid.NewGuid();
        public Guid SourceId { get; } = Guid.NewGuid();
        public Guid TagId { get; } = Guid.NewGuid();
        public List<TaxExpense> Expenses { get; } = [];
        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<TaxExpense>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TaxExpense>>(Expenses);

        public Task<IReadOnlyList<TaxExpense>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TaxExpense>>(Expenses);

        public Task<IReadOnlyList<TaxExpense>> GetAllForExportAsync(bool includeSoftDeleted, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TaxExpense>>(Expenses.Where(x => includeSoftDeleted || !x.IsDeleted).ToList());

        public Task<TaxExpense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Expenses.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

        public Task<TaxExpense?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Expenses.FirstOrDefault(x => x.Id == id));

        public Task<TaxExpense?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
            GetByIdIncludingDeletedAsync(id, cancellationToken);

        public Task<TaxExpense?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
            GetByIdIncludingDeletedAsync(id, cancellationToken);

        public Task<TaxExpense?> GetByIdForUpdateIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) =>
            GetByIdIncludingDeletedAsync(id, cancellationToken);

        public Task<bool> SourceExistsAsync(Guid sourceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(sourceId == SourceId);

        public Task<bool> BankExistsAsync(Guid bankId, CancellationToken cancellationToken = default) =>
            Task.FromResult(bankId == BankId);

        public Task<IReadOnlyList<Guid>> GetExistingTagIdsAsync(IReadOnlyList<Guid> tagIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(tagIds.Where(x => x == TagId).ToList());

        public Task AddAsync(TaxExpense entity, CancellationToken cancellationToken = default)
        {
            Expenses.Add(entity);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }

        public Task<PagedResult<TaxExpense>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<decimal> GetTotalSpentAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ExpenseTotalByGroup>> GetTotalByBankAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ExpenseTotalByGroup>> GetTotalBySourceAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<TaxExpense>> FilterWithDetailsAsync(ExpenseFilterQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
