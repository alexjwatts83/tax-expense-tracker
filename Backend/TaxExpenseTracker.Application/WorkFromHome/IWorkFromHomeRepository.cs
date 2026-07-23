using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.WorkFromHome;

public interface IWorkFromHomeRepository : ISoftDeleteRepository<WorkFromHomeEntry>
{
    Task<IReadOnlyList<WorkFromHomeEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<bool> ExistsForDateAsync(DateTime workDate, Guid? excludingId = null, CancellationToken cancellationToken = default);
}