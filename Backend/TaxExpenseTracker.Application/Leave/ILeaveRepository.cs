using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.Leave;

public interface ILeaveRepository : ISoftDeleteRepository<LeaveEntry>
{
    Task<IReadOnlyList<LeaveEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<bool> ExistsForDateAsync(DateTime leaveDate, Guid? excludingId = null, CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<LeaveEntry>> GetByDatesAsync(
        IReadOnlyCollection<DateTime> leaveDates,
        CancellationToken cancellationToken = default)
    {
        if (leaveDates.Count == 0)
            return [];

        var dates = leaveDates.Select(x => x.Date).ToHashSet();
        var entries = await GetByDateRangeAsync(dates.Min(), dates.Max(), cancellationToken);
        return entries.Where(x => dates.Contains(x.LeaveDate.Date)).ToList();
    }
}