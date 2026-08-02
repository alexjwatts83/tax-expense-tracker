using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.PublicHolidays;

public interface IPublicHolidayRepository : IRepository<PublicHoliday>
{
    Task<IReadOnlyList<PublicHoliday>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task RemoveByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<PublicHoliday>> GetAllForUpdateAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(cancellationToken);
    }

    async Task<IReadOnlyList<PublicHoliday>> GetByIdsForUpdateAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var entities = new List<PublicHoliday>();
        foreach (var id in ids.Distinct())
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity is not null)
                entities.Add(entity);
        }

        return entities;
    }
}
