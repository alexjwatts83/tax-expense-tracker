namespace TaxExpenseTracker.Domain.Entities;

internal static class DayEntryHours
{
    public static decimal Resolve(DayEntryType entryType, decimal? specificHours)
    {
        return entryType switch
        {
            DayEntryType.FullDay => 7.6m,
            DayEntryType.HalfDay => 3.8m,
            DayEntryType.SpecificHours => ValidateSpecificHours(specificHours),
            _ => throw new ArgumentOutOfRangeException(nameof(entryType), "Entry type is invalid."),
        };
    }

    private static decimal ValidateSpecificHours(decimal? specificHours)
    {
        if (!specificHours.HasValue || specificHours.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(specificHours), "Specific hours must be greater than 0.");

        if (specificHours.Value > 24)
            throw new ArgumentOutOfRangeException(nameof(specificHours), "Specific hours must be 24 or less.");

        return specificHours.Value;
    }
}
