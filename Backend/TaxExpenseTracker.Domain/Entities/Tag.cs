namespace TaxExpenseTracker.Domain.Entities;

public class Tag : SoftDeletableEntity
{
    public const string DefaultColor = "#CBD5E1";

    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = DefaultColor;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TaxExpenseTag> TaxExpenseTags { get; set; } = new List<TaxExpenseTag>();

    public static Tag Create(string name, string? color, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return new Tag
        {
            Id = Guid.NewGuid(),
            Name = NormalizeRequired(name, nameof(Name)),
            Color = NormalizeColor(color),
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            IsDeleted = false,
        };
    }

    public void Rename(string name)
    {
        Name = NormalizeRequired(name, nameof(Name));
    }

    public void SetColor(string? color)
    {
        Color = NormalizeColor(color);
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