namespace TaxExpenseTracker.Domain.Entities;

public class Tag : AuditableSoftDeletableEntity
{
    public const string DefaultColor = "#CBD5E1";

    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = DefaultColor;

    public ICollection<TaxExpenseTag> TaxExpenseTags { get; private set; } = new List<TaxExpenseTag>();

    public static Tag Create(string name, string? color, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return new Tag
        {
            Id = Guid.NewGuid(),
            Name = NormalizeRequired(name, nameof(Name)),
            Color = NormalizeColor(color),
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        };
    }

    public static Tag Import(Guid id, string name, string? color, TimeProvider timeProvider, DateTime? createdAt = null, DateTime? updatedAt = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty, nameof(id));
        var tag = Create(name, color, timeProvider);
        tag.Id = id;
        tag.RestoreAuditTimestamps(createdAt, updatedAt);
        return tag;
    }

    public void Update(string name, string? color, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var normalizedName = NormalizeRequired(name, nameof(Name));
        Name = normalizedName;
        Color = NormalizeColor(color);
        UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
    }

    public void ApplyImport(string name, string? color, TimeProvider timeProvider, DateTime? createdAt, DateTime? updatedAt)
    {
        Update(name, color, timeProvider);
        RestoreAuditTimestamps(createdAt, updatedAt);
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, fieldName);
        return value.Trim();
    }

    private static string NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return DefaultColor;
        }

        var trimmed = color.Trim();
        if (trimmed.Length != 7 || trimmed[0] != '#')
        {
            return DefaultColor;
        }

        return trimmed.ToUpperInvariant();
    }
}