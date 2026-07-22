using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Banks;

public sealed class BankService : IBankService
{
    private readonly IBankRepository _bankRepository;

    public BankService(IBankRepository bankRepository)
    {
        _bankRepository = bankRepository;
    }

    public async Task<IReadOnlyList<BankReadDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var banks = await _bankRepository.GetAllAsync(cancellationToken);

        return banks
            .OrderBy(b => b.Name)
            .Select(b => new BankReadDto(b.Id, b.Name, b.CreatedAt))
            .ToList();
    }

    public async Task<BankReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bank = await _bankRepository.GetByIdAsync(id, cancellationToken);

        return bank is null
            ? null
            : new BankReadDto(bank.Id, bank.Name, bank.CreatedAt);
    }

    public async Task<BankReadDto> CreateAsync(CreateBankCommand command, CancellationToken cancellationToken = default)
    {
        var bank = Bank.Create(command.Name);

        await _bankRepository.AddAsync(bank, cancellationToken);
        await _bankRepository.SaveChangesAsync(cancellationToken);

        return new BankReadDto(bank.Id, bank.Name, bank.CreatedAt);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateBankCommand command, CancellationToken cancellationToken = default)
    {
        var bank = await _bankRepository.GetByIdAsync(id, cancellationToken);
        if (bank is null)
        {
            return false;
        }

        bank.Rename(command.Name);
        await _bankRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bank = await _bankRepository.GetByIdAsync(id, cancellationToken);
        if (bank is null)
        {
            return false;
        }

        bank.SoftDelete();
        await _bankRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bank = await _bankRepository.GetByIdIncludingDeletedAsync(id, cancellationToken);
        if (bank is null || !bank.IsDeleted)
        {
            return false;
        }

        bank.Restore();
        await _bankRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}