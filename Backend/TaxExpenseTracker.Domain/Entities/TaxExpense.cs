namespace TaxExpenseTracker.Domain.Entities;

public class TaxExpense
{
    public Guid Id { get; set; }
    public string Item { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Bank { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid SourceId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Tracker? Source { get; set; }
    public ICollection<TaxExpenseTag> TaxExpenseTags { get; set; } = new List<TaxExpenseTag>();

    public static TaxExpense Create(
        string item,
        string? description,
        DateTime date,
        string bank,
        decimal price,
        Guid sourceId,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        return new TaxExpense
        {
            Id = Guid.NewGuid(),
            Item = NormalizeRequired(item, nameof(Item)),
            Description = NormalizeOptional(description),
            Date = ValidateDate(date),
            Bank = NormalizeRequired(bank, nameof(Bank)),
            Price = ValidatePrice(price),
            SourceId = ValidateSourceId(sourceId),
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        };
    }

    public void UpdateDetails(
        string item,
        string? description,
        DateTime date,
        string bank,
        decimal price,
        Guid sourceId,
        DateTime? utcNow = null)
    {
        Item = NormalizeRequired(item, nameof(Item));
        Description = NormalizeOptional(description);
        Date = ValidateDate(date);
        Bank = NormalizeRequired(bank, nameof(Bank));
        Price = ValidatePrice(price);
        SourceId = ValidateSourceId(sourceId);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
    }

    public void SoftDelete(DateTime? utcNow = null)
    {
        IsDeleted = true;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
    }

    public void Restore(DateTime? utcNow = null)
    {
        IsDeleted = false;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        return normalized;
    }

    private static string NormalizeOptional(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static decimal ValidatePrice(decimal price)
    {
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be non-negative.");
        }

        return price;
    }

    private static DateTime ValidateDate(DateTime date)
    {
        if (date == default)
        {
            throw new ArgumentException("Date is required.", nameof(date));
        }

        return date;
    }

    private static Guid ValidateSourceId(Guid sourceId)
    {
        if (sourceId == Guid.Empty)
        {
            throw new ArgumentException("SourceId is required.", nameof(sourceId));
        }

        return sourceId;
    }
}