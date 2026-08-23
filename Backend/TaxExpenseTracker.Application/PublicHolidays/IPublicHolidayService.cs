namespace TaxExpenseTracker.Application.PublicHolidays;

public interface IPublicHolidayService
{
    Task<IReadOnlyList<PublicHolidayReadDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicHolidayReadDto>> GetByDateRangeAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);
    Task<PublicHolidayImportResultDto> ImportAsync(string csvContent, string? source, CancellationToken cancellationToken = default);
    Task<PublicHolidayReadDto?> SetWorkableAsync(Guid holidayId, bool canBeWorkedOn, CancellationToken cancellationToken = default);
    Task<PublicHolidayReadDto?> UpdateAsync(Guid holidayId, UpdatePublicHolidayCommand command, CancellationToken cancellationToken = default);
}
