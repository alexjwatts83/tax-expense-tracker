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
        if (query.Price.HasValue && query.Price.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Price must be non-negative.");
        }
    }
}
