namespace TaxExpenseTracker.Application.WorkFromHome;

public interface IWorkFromHomeService
{
    Task<IReadOnlyList<WorkFromHomeReadDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkFromHomeReadDto>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<WorkFromHomeReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkFromHomeReadDto> CreateAsync(CreateWorkFromHomeCommand command, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpdateWorkFromHomeCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}