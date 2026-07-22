namespace TaxExpenseTracker.Application.Banks;

public interface IBankService
{
    Task<IReadOnlyList<BankReadDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BankReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BankReadDto> CreateAsync(CreateBankCommand command, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpdateBankCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}