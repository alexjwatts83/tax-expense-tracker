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
    public async Task ImportAsync_MissingSource_ReturnsReferenceError()
    {
        var repository = new DataTransferExpenseRepository();
        var handler = new DataTransferExpenseImportHandler(repository, TestTime.TimeProvider);

        var results = await handler.ImportAsync(
            CreatePayload(repository, sourceId: Guid.NewGuid()),
            new DataTransferImportOptions(DryRun: true));

        var result = Assert.Single(results, x => x.Entity == "expenses");
        var error = Assert.Single(result.Errors);
        Assert.Equal("ERR_REFERENCE_NOT_FOUND", error.Code);
        Assert.Contains("SourceId", error.Message);
    }

    [Fact]
    public async Task ImportAsync_MissingBank_ReturnsReferenceError()
    {
        var repository = new DataTransferExpenseRepository();
        var handler = new DataTransferExpenseImportHandler(repository, TestTime.TimeProvider);

        var results = await handler.ImportAsync(
            CreatePayload(repository, bankId: Guid.NewGuid()),
            new DataTransferImportOptions(DryRun: true));

        var result = Assert.Single(results, x => x.Entity == "expenses");
        var error = Assert.Single(result.Errors);
        Assert.Equal("ERR_REFERENCE_NOT_FOUND", error.Code);
        Assert.Contains("BankId", error.Message);
    }

    [Fact]
    public async Task ImportAsync_MissingTag_ReturnsReferenceError()
    {
        var repository = new DataTransferExpenseRepository();
        var handler = new DataTransferExpenseImportHandler(repository, TestTime.TimeProvider);
        var expenseId = Guid.NewGuid();
        var payload = CreatePayload(repository, expenseId, tagId: Guid.NewGuid());

        var results = await handler.ImportAsync(payload, new DataTransferImportOptions(DryRun: true));

        var expenseResult = Assert.Single(results, x => x.Entity == "expenses");
        var tagResult = Assert.Single(results, x => x.Entity == "expenseTags");
        Assert.Empty(expenseResult.Errors);
        var error = Assert.Single(tagResult.Errors);
        Assert.Equal("ERR_REFERENCE_NOT_FOUND", error.Code);
        Assert.Contains("TagId", error.Message);
    }

    [Fact]
    public async Task ImportAsync_Upsert_CreatesMissingExpense()
    {
        var repository = new DataTransferExpenseRepository();
        var handler = new DataTransferExpenseImportHandler(repository, TestTime.TimeProvider);
        var expenseId = Guid.NewGuid();

        var results = await handler.ImportAsync(
            CreatePayload(repository, expenseId),
            new DataTransferImportOptions(DataTransferImportMode.Upsert));

        var result = Assert.Single(results, x => x.Entity == "expenses");
        var expense = Assert.Single(repository.Expenses);
        Assert.Equal(expenseId, expense.Id);
        Assert.Equal("Laptop", expense.Description);
        Assert.Equal(1, result.CreatedCount);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ImportAsync_Upsert_UpdatesAndRestoresExistingExpense()
    {
        var repository = new DataTransferExpenseRepository();
        var existing = TaxExpense.Create("Old", new DateTime(2026, 7, 1), repository.BankId, 10m, repository.SourceId, TestTime.TimeProvider);
        existing.SoftDelete(TestTime.TimeProvider);
        repository.Expenses.Add(existing);
        var handler = new DataTransferExpenseImportHandler(repository, TestTime.TimeProvider);

        var results = await handler.ImportAsync(
            CreatePayload(repository, existing.Id),
            new DataTransferImportOptions(DataTransferImportMode.Upsert));

        var result = Assert.Single(results, x => x.Entity == "expenses");
        Assert.Equal("Laptop", existing.Description);
        Assert.Equal(1200m, existing.Price);
        Assert.False(existing.IsDeleted);
        Assert.Equal(1, result.UpdatedCount);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ImportAsync_InsertOnly_SkipsExistingExpenseWithoutMutation()
    {
        var repository = new DataTransferExpenseRepository();
        var existing = TaxExpense.Create("Original", new DateTime(2026, 7, 1), repository.BankId, 10m, repository.SourceId, TestTime.TimeProvider);
        repository.Expenses.Add(existing);
        var handler = new DataTransferExpenseImportHandler(repository, TestTime.TimeProvider);

        var results = await handler.ImportAsync(
            CreatePayload(repository, existing.Id),
            new DataTransferImportOptions(DataTransferImportMode.InsertOnly));

        var result = Assert.Single(results, x => x.Entity == "expenses");
        Assert.Equal("Original", existing.Description);
        Assert.Equal(10m, existing.Price);
        Assert.Equal(1, result.SkippedCount);
        Assert.Contains(result.Warnings, x => x.Code == "WARN_INSERT_ONLY_SKIPPED");
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

    private static ExpenseImportPayloadDto CreatePayload(
        DataTransferExpenseRepository repository,
        Guid? expenseId = null,
        Guid? bankId = null,
        Guid? sourceId = null,
        Guid? tagId = null)
    {
        var resolvedExpenseId = expenseId ?? Guid.NewGuid();
        return new ExpenseImportPayloadDto(
            [new ExpenseImportItemDto(
                resolvedExpenseId,
                TestTime.FixedUtcNow.UtcDateTime,
                "Laptop",
                1200m,
                bankId ?? repository.BankId,
                sourceId ?? repository.SourceId,
                null,
                null,
                false)],
            tagId.HasValue
                ? [new ExpenseTagImportItemDto(Guid.NewGuid(), resolvedExpenseId, tagId.Value)]
                : []);
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
