namespace TaxExpenseTracker.Domain.Entities;

public class TaxExpense : AuditableSoftDeletableEntity
{
    private readonly List<TaxExpenseTag> _taxExpenseTags = [];

    public string Description { get; private set; } = string.Empty;
    public DateOnly Date { get; private set; }
    public Guid BankId { get; private set; }
    public decimal Price { get; private set; }
    public Guid SourceId { get; private set; }

    public Bank? Bank { get; set; }
    public Tracker? Source { get; set; }
    public IReadOnlyCollection<TaxExpenseTag> TaxExpenseTags => _taxExpenseTags;

    public static TaxExpense Create(
        string? description,
        DateOnly date,
        Guid bankId,
        decimal price,
        Guid sourceId,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var now = timeProvider.GetUtcNow().UtcDateTime;

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

    // Rehydrates an expense with a caller-supplied identity for import/restore scenarios.
    public static TaxExpense Import(
        Guid id,
        string? description,
        DateOnly date,
        Guid bankId,
        decimal price,
        Guid sourceId,
        TimeProvider timeProvider)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty, nameof(id));
        var expense = Create(description, date, bankId, price, sourceId, timeProvider);
        expense.Id = id;
        return expense;
    }

    public void UpdateDetails(
        string? description,
        DateOnly date,
        Guid bankId,
        decimal price,
        Guid sourceId,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        Description = NormalizeOptional(description);
        Date = ValidateDate(date);
        BankId = ValidateBankId(bankId);
        Price = ValidatePrice(price);
        SourceId = ValidateSourceId(sourceId);
        UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
    }

    // Reconciles tag links to exactly the requested set, preserving existing link identities.
    public void ReplaceTagLinks(IEnumerable<Guid> tagIds)
    {
        ArgumentNullException.ThrowIfNull(tagIds);
        var desired = tagIds.Where(id => id != Guid.Empty).ToHashSet();

        _taxExpenseTags.RemoveAll(link => !desired.Contains(link.TagId));

        var existing = _taxExpenseTags.Select(link => link.TagId).ToHashSet();
        foreach (var tagId in desired.Where(id => !existing.Contains(id)))
            _taxExpenseTags.Add(TaxExpenseTag.Create(Id, tagId));
    }

    public void AddTagLink(Guid tagId, Guid? linkId = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tagId, Guid.Empty, nameof(tagId));
        if (_taxExpenseTags.Any(link => link.TagId == tagId))
            return;

        var link = TaxExpenseTag.Create(Id, tagId);
        if (linkId.HasValue && linkId.Value != Guid.Empty)
            link.Id = linkId.Value;

        _taxExpenseTags.Add(link);
    }

    public bool RemoveTagLink(Guid tagId)
    {
        return _taxExpenseTags.RemoveAll(link => link.TagId == tagId) > 0;
    }

    public bool HasTagLink(Guid tagId)
    {
        return _taxExpenseTags.Any(link => link.TagId == tagId);
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

    private static DateOnly ValidateDate(DateOnly date)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(date, default, nameof(date));
        return date;
    }

    private static Guid ValidateSourceId(Guid sourceId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(sourceId, Guid.Empty, nameof(sourceId));
        return sourceId;
    }

    private static Guid ValidateBankId(Guid bankId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(bankId, Guid.Empty, nameof(bankId));
        return bankId;
    }
}