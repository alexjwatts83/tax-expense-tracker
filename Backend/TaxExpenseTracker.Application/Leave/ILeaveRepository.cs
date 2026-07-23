using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.Leave;

public interface ILeaveRepository : ISoftDeleteRepository<LeaveEntry>
{
    Task<IReadOnlyList<LeaveEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<bool> ExistsForDateAsync(DateTime leaveDate, Guid? excludingId = null, CancellationToken cancellationToken = default);
}