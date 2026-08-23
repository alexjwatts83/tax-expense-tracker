using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.WorkLocation;

public interface IWorkLocationRepository : ISoftDeleteRepository<WorkLocationEntry>
{
    Task<IReadOnlyList<WorkLocationEntry>> GetByDateRangeAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);
    Task<bool> ExistsForDateAsync(DateOnly workDate, Guid? excludingId = null, CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<WorkLocationEntry>> GetByDatesAsync(
        IReadOnlyCollection<DateOnly> workDates,
        CancellationToken cancellationToken = default)
    {
        if (workDates.Count == 0)
            return [];

        var dates = workDates.ToHashSet();
        var entries = await GetByDateRangeAsync(dates.Min(), dates.Max(), cancellationToken);
        return entries.Where(x => dates.Contains(x.WorkDate)).ToList();
    }
}