using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.WorkLocation;

public interface IWorkLocationRepository : ISoftDeleteRepository<WorkLocationEntry>
{
    Task<IReadOnlyList<WorkLocationEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<bool> ExistsForDateAsync(DateTime workDate, Guid? excludingId = null, CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<WorkLocationEntry>> GetByDatesAsync(
        IReadOnlyCollection<DateTime> workDates,
        CancellationToken cancellationToken = default)
    {
        if (workDates.Count == 0)
            return [];

        var dates = workDates.Select(x => x.Date).ToHashSet();
        var entries = await GetByDateRangeAsync(dates.Min(), dates.Max(), cancellationToken);
        return entries.Where(x => dates.Contains(x.WorkDate.Date)).ToList();
    }
}