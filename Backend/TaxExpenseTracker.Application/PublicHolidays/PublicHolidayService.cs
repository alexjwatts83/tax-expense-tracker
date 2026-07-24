using System.Globalization;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.PublicHolidays;

public sealed class PublicHolidayService : IPublicHolidayService
{
    private readonly IPublicHolidayRepository _publicHolidayRepository;
    private readonly TimeProvider _timeProvider;

    public PublicHolidayService(IPublicHolidayRepository publicHolidayRepository, TimeProvider timeProvider)
    {
        _publicHolidayRepository = publicHolidayRepository;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<PublicHolidayReadDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var holidays = await _publicHolidayRepository.GetAllAsync(cancellationToken);

        return holidays
            .OrderBy(x => x.HolidayDate)
            .ThenBy(x => x.Name)
            .Select(ToReadDto)
            .ToList();
    }

    public async Task<IReadOnlyList<PublicHolidayReadDto>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var holidays = await _publicHolidayRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);

        return holidays
            .OrderBy(x => x.HolidayDate)
            .ThenBy(x => x.Name)
            .Select(ToReadDto)
            .ToList();
    }

    public async Task<PublicHolidayImportResultDto> ImportAsync(string csvContent, string? source, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            throw new ArgumentException("CSV content is required.", nameof(csvContent));
        }

        var normalizedSource = string.IsNullOrWhiteSpace(source) ? "CSV Import" : source.Trim();

        var parseResult = Parse(csvContent);

        if (parseResult.Errors.Count > 0)
        {
            throw new ArgumentException("CSV validation failed: " + string.Join(" | ", parseResult.Errors));
        }

        if (parseResult.Rows.Count == 0)
        {
            return new PublicHolidayImportResultDto(0, 0, ["CSV contained no data rows."]);
        }

        DateTime minDate = parseResult.Rows.Min(x => x.Date);
        DateTime maxDate = parseResult.Rows.Max(x => x.Date);

        var existingInRange = await _publicHolidayRepository.GetByDateRangeAsync(minDate, maxDate, cancellationToken);
        var existingKeys = existingInRange
            .Select(x => ToKey(x.HolidayDate, x.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;
        var skipped = 0;

        foreach (var row in parseResult.Rows)
        {
            var key = ToKey(row.Date, row.Name);

            if (existingKeys.Contains(key))
            {
                skipped++;
                continue;
            }

            var holiday = PublicHoliday.Create(row.Date, row.Name, normalizedSource, true, _timeProvider, row.CanBeWorkedOn);
            await _publicHolidayRepository.AddAsync(holiday, cancellationToken);
            existingKeys.Add(key);
            added++;
        }

        if (added > 0)
        {
            await _publicHolidayRepository.SaveChangesAsync(cancellationToken);
        }

        var warnings = new List<string>();
        if (skipped > 0)
        {
            warnings.Add($"Skipped {skipped} duplicate row(s).");
        }

        return new PublicHolidayImportResultDto(added, skipped, warnings);
    }

    public async Task<PublicHolidayReadDto?> SetWorkableAsync(Guid holidayId, bool canBeWorkedOn, CancellationToken cancellationToken = default)
    {
        var holiday = await _publicHolidayRepository.GetByIdAsync(holidayId, cancellationToken);
        if (holiday is null)
        {
            return null;
        }

        holiday.SetWorkable(canBeWorkedOn);
        await _publicHolidayRepository.SaveChangesAsync(cancellationToken);

        return ToReadDto(holiday);
    }

    private static string ToKey(DateTime date, string name)
    {
        return $"{date.Date:yyyy-MM-dd}|{name.Trim()}";
    }

    private static PublicHolidayReadDto ToReadDto(PublicHoliday holiday)
    {
        return new PublicHolidayReadDto(
            holiday.Id,
            holiday.HolidayDate,
            holiday.Name,
            holiday.Source,
            holiday.IsImported,
            holiday.CanBeWorkedOn,
            holiday.CreatedAt);
    }

    private static CsvParseResult Parse(string csvContent)
    {
        using var reader = new StringReader(csvContent);

        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return new CsvParseResult([], ["Header row is required."]);
        }

        var headers = SplitCsvLine(headerLine);
        var dateIndex = FindHeaderIndex(headers, "date", "holidaydate", "holiday_date");
        var nameIndex = FindHeaderIndex(headers, "name", "holidayname", "holiday_name");
        var workableIndex = FindHeaderIndex(headers, "canbeworkedon", "workable", "isworkable", "allowwork");

        var errors = new List<string>();

        if (dateIndex < 0)
        {
            errors.Add("Missing required Date column (accepted: Date, HolidayDate).");
        }

        if (nameIndex < 0)
        {
            errors.Add("Missing required Name column (accepted: Name, HolidayName).");
        }

        if (errors.Count > 0)
        {
            return new CsvParseResult([], errors);
        }

        var rows = new List<CsvHolidayRow>();
        var seenImportKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? line;
        var lineNumber = 1;

        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = SplitCsvLine(line);
            var dateValue = GetValue(values, dateIndex)?.Trim();
            var nameValue = GetValue(values, nameIndex)?.Trim();
            var workableValue = GetValue(values, workableIndex)?.Trim();

            if (string.IsNullOrWhiteSpace(dateValue))
            {
                errors.Add($"Line {lineNumber}: Date is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(nameValue))
            {
                errors.Add($"Line {lineNumber}: Name is required.");
                continue;
            }

            if (!TryParseDate(dateValue, out var date))
            {
                errors.Add($"Line {lineNumber}: Invalid date '{dateValue}'. Use yyyy-MM-dd or dd/MM/yyyy.");
                continue;
            }

            if (!TryParseWorkableValue(workableValue, workableIndex >= 0, out var canBeWorkedOn))
            {
                errors.Add($"Line {lineNumber}: Invalid workable value '{workableValue}'. Use true/false, yes/no, or 1/0.");
                continue;
            }

            var importKey = ToKey(date, nameValue);
            if (!seenImportKeys.Add(importKey))
            {
                continue;
            }

            rows.Add(new CsvHolidayRow(date, nameValue, canBeWorkedOn));
        }

        return new CsvParseResult(rows, errors);
    }

    private static int FindHeaderIndex(IReadOnlyList<string> headers, params string[] acceptedNames)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            var normalized = NormalizeHeader(headers[i]);
            if (acceptedNames.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string NormalizeHeader(string value)
    {
        return value.Trim().Replace(" ", string.Empty).Replace("_", string.Empty);
    }

    private static string? GetValue(IReadOnlyList<string> values, int index)
    {
        return index >= 0 && index < values.Count ? values[index] : null;
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        string[] formats = ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "yyyy/M/d"];
        return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static bool TryParseWorkableValue(string? value, bool columnExists, out bool canBeWorkedOn)
    {
        canBeWorkedOn = false;

        if (!columnExists || string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "true" or "yes" or "y" or "1" => SetResult(true, out canBeWorkedOn),
            "false" or "no" or "n" or "0" => SetResult(false, out canBeWorkedOn),
            _ => false,
        };
    }

    private static bool SetResult(bool value, out bool result)
    {
        result = value;
        return true;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        values.Add(current.ToString());
        return values;
    }

    private sealed record CsvHolidayRow(DateTime Date, string Name, bool CanBeWorkedOn);

    private sealed record CsvParseResult(IReadOnlyList<CsvHolidayRow> Rows, IReadOnlyList<string> Errors);
}
