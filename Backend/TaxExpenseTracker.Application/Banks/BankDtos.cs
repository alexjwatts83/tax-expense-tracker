namespace TaxExpenseTracker.Application.Banks;

public sealed record BankReadDto(Guid Id, string Name, DateTime CreatedAt);

public sealed record CreateBankCommand(string Name);

public sealed record UpdateBankCommand(string Name);