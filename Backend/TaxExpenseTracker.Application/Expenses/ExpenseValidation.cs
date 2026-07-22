namespace TaxExpenseTracker.Application.Expenses;

internal static class ExpenseValidation
{
    public static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        return (normalizedPage, normalizedPageSize);
    }

    public static void ValidateFilter(ExpenseFilterQuery query)
    {
        if (query.StartDate.HasValue && query.EndDate.HasValue && query.StartDate > query.EndDate)
        {
            throw new ArgumentException("StartDate cannot be after EndDate.", nameof(query));
        }

        if (query.MinPrice.HasValue && query.MinPrice.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "MinPrice must be non-negative.");
        }

        if (query.MaxPrice.HasValue && query.MaxPrice.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "MaxPrice must be non-negative.");
        }

        if (query.MinPrice.HasValue && query.MaxPrice.HasValue && query.MinPrice > query.MaxPrice)
        {
            throw new ArgumentException("MinPrice cannot be greater than MaxPrice.", nameof(query));
        }
    }
}
