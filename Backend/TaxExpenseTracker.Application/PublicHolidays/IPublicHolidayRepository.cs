using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.PublicHolidays;

public interface IPublicHolidayRepository : IRepository<PublicHoliday>
{
    Task<IReadOnlyList<PublicHoliday>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
}
