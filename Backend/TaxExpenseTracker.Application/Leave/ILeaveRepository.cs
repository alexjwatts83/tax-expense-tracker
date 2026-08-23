using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.Leave;

public interface ILeaveRepository : ISoftDeleteRepository<LeaveEntry>
{
    Task<IReadOnlyList<LeaveEntry>> GetByDateRangeAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);
    Task<bool> ExistsForDateAsync(DateOnly leaveDate, Guid? excludingId = null, CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<LeaveEntry>> GetByDatesAsync(
        IReadOnlyCollection<DateOnly> leaveDates,
        CancellationToken cancellationToken = default)
    {
        if (leaveDates.Count == 0)
            return [];

        var dates = leaveDates.ToHashSet();
        var entries = await GetByDateRangeAsync(dates.Min(), dates.Max(), cancellationToken);
        return entries.Where(x => dates.Contains(x.LeaveDate)).ToList();
    }
}