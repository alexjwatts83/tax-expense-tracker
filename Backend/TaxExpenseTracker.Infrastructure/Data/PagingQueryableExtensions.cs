using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Infrastructure.Data;

public static class PagingQueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Max(pageSize, 1);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = normalizedPage,
            PageSize = normalizedPageSize
        };
    }
}
