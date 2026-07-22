using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Expenses;

public sealed class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;

    public ExpenseService(IExpenseRepository expenseRepository)
    {
        _expenseRepository = expenseRepository;
    }

    public async Task<IReadOnlyList<ExpenseReadDto>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize) = ExpenseValidation.NormalizePaging(page, pageSize);

        var expenses = await _expenseRepository.GetPagedWithDetailsAsync(
            (normalizedPage - 1) * normalizedPageSize,
            normalizedPageSize,
            cancellationToken);
        return expenses.Select(MapExpense).ToList();
    }

    public async Task<ExpenseReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        return expense is null ? null : MapExpense(expense);
    }

    public async Task<ExpenseReadDto> CreateAsync(CreateExpenseCommand command, CancellationToken cancellationToken = default)
    {
        var sourceExists = await _expenseRepository.SourceExistsAsync(command.SourceId, cancellationToken);
        if (!sourceExists)
        {
            throw new InvalidOperationException("Source tracker does not exist.");
        }

        var bankExists = await _expenseRepository.BankExistsAsync(command.BankId, cancellationToken);
        if (!bankExists)
        {
            throw new InvalidOperationException("Bank does not exist.");
        }

        var validTagIds = await _expenseRepository.GetExistingTagIdsAsync(command.TagIds, cancellationToken);

        var expense = TaxExpense.Create(
            command.Description,
            command.Date,
            command.BankId,
            command.Price,
            command.SourceId);

        expense.TaxExpenseTags = validTagIds
            .Select(tagId => TaxExpenseTag.Create(expense.Id, tagId))
            .ToList();

        await _expenseRepository.AddAsync(expense, cancellationToken);
        await _expenseRepository.SaveChangesAsync(cancellationToken);

        var created = await _expenseRepository.GetByIdWithDetailsAsync(expense.Id, cancellationToken)
            ?? throw new InvalidOperationException("Created expense could not be loaded.");

        return MapExpense(created);
    }

    public async Task<ExpenseReadDto?> UpdateAsync(Guid id, UpdateExpenseCommand command, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (expense is null)
        {
            return null;
        }

        var sourceExists = await _expenseRepository.SourceExistsAsync(command.SourceId, cancellationToken);
        if (!sourceExists)
        {
            throw new InvalidOperationException("Source tracker does not exist.");
        }

        var bankExists = await _expenseRepository.BankExistsAsync(command.BankId, cancellationToken);
        if (!bankExists)
        {
            throw new InvalidOperationException("Bank does not exist.");
        }

        var validTagIds = await _expenseRepository.GetExistingTagIdsAsync(command.TagIds, cancellationToken);

        expense.UpdateDetails(
            command.Description,
            command.Date,
            command.BankId,
            command.Price,
            command.SourceId);

        expense.TaxExpenseTags.Clear();
        foreach (var tagId in validTagIds)
        {
            expense.TaxExpenseTags.Add(TaxExpenseTag.Create(expense.Id, tagId));
        }

        await _expenseRepository.SaveChangesAsync(cancellationToken);

        var updated = await _expenseRepository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Updated expense could not be loaded.");

        return MapExpense(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdForUpdateAsync(id, cancellationToken);
        if (expense is null)
        {
            return false;
        }

        expense.SoftDelete();
        await _expenseRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdForRestoreAsync(id, cancellationToken);
        if (expense is null || !expense.IsDeleted)
        {
            return false;
        }

        expense.Restore();
        await _expenseRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<ExpenseSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalSpent = await _expenseRepository.GetTotalSpentAsync(cancellationToken);
        var byBank = await _expenseRepository.GetTotalByBankAsync(cancellationToken);
        var bySource = await _expenseRepository.GetTotalBySourceAsync(cancellationToken);

        return new ExpenseSummaryDto(totalSpent, byBank, bySource);
    }

    public async Task<IReadOnlyList<ExpenseReadDto>> FilterAsync(ExpenseFilterQuery query, CancellationToken cancellationToken = default)
    {
        ExpenseValidation.ValidateFilter(query);

        var expenses = await _expenseRepository.FilterWithDetailsAsync(query, cancellationToken);
        return expenses.Select(MapExpense).ToList();
    }

    private static ExpenseReadDto MapExpense(TaxExpense expense)
    {
        return new ExpenseReadDto(
            expense.Id,
            expense.Description,
            expense.Date,
            expense.BankId,
            expense.Bank is null
                ? null
                : new ExpenseBankDto(expense.Bank.Id, expense.Bank.Name, expense.Bank.CreatedAt),
            expense.Price,
            expense.SourceId,
            expense.Source is null
                ? null
                : new ExpenseSourceDto(expense.Source.Id, expense.Source.Name, expense.Source.Description, expense.Source.CreatedAt),
            expense.TaxExpenseTags
                .Where(x => x.Tag is not null)
                .Select(x => new ExpenseTagDto(x.Tag!.Id, x.Tag.Name, x.Tag.CreatedAt))
                .ToList(),
            expense.CreatedAt,
            expense.UpdatedAt);
    }
}
