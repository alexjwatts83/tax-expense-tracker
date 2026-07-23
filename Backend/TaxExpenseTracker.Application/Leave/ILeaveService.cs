using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.Leave;

public interface ILeaveService
{
    Task<IReadOnlyList<LeaveReadDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveReadDto>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<LeaveReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LeaveReadDto> CreateAsync(CreateLeaveCommand command, CancellationToken cancellationToken = default);
    Task<BatchCreateLeaveResult> BatchCreateAsync(IReadOnlyList<CreateLeaveCommand> commands, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpdateLeaveCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DayEntrySummaryDto> GetSummaryAsync(SummaryView view, DateTime date, CancellationToken cancellationToken = default);
}