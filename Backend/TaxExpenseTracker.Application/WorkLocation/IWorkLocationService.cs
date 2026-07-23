using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.WorkLocation;

public interface IWorkLocationService
{
    Task<IReadOnlyList<WorkLocationReadDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkLocationReadDto>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<WorkLocationReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkLocationReadDto> CreateAsync(CreateWorkLocationCommand command, CancellationToken cancellationToken = default);
    Task<BatchCreateWorkLocationResult> BatchCreateAsync(IReadOnlyList<CreateWorkLocationCommand> commands, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpdateWorkLocationCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DayEntrySummaryDto> GetSummaryAsync(SummaryView view, DateTime date, CancellationToken cancellationToken = default);
}