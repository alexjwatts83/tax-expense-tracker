using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.WorkLocation;

public interface IWorkLocationRepository : ISoftDeleteRepository<WorkLocationEntry>
{
    Task<IReadOnlyList<WorkLocationEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<bool> ExistsForDateAsync(DateTime workDate, Guid? excludingId = null, CancellationToken cancellationToken = default);
}