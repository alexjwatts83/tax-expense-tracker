namespace TaxExpenseTracker.Domain.Entities;

public class TaxExpense
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid BankId { get; set; }
    public decimal Price { get; set; }
    public Guid SourceId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Bank? Bank { get; set; }
    public Tracker? Source { get; set; }
    public ICollection<TaxExpenseTag> TaxExpenseTags { get; set; } = new List<TaxExpenseTag>();

    public static TaxExpense Create(
        string? description,
        DateTime date,
        Guid bankId,
        decimal price,
        Guid sourceId,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        return new TaxExpense
        {
            Id = Guid.NewGuid(),
            Description = NormalizeOptional(description),
            Date = ValidateDate(date),
            BankId = ValidateBankId(bankId),
            Price = ValidatePrice(price),
            SourceId = ValidateSourceId(sourceId),
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        };
    }

    public void UpdateDetails(
        string? description,
        DateTime date,
        Guid bankId,
        decimal price,
        Guid sourceId,
        DateTime? utcNow = null)
    {
        Description = NormalizeOptional(description);
        Date = ValidateDate(date);
        BankId = ValidateBankId(bankId);
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

    private static Guid ValidateBankId(Guid bankId)
    {
        if (bankId == Guid.Empty)
        {
            throw new ArgumentException("BankId is required.", nameof(bankId));
        }

        return bankId;
    }
}