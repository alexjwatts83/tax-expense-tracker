using Microsoft.AspNetCore.Mvc;
using TaxExpenseTracker.Api.Models;
using TaxExpenseTracker.Application.PublicHolidays;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/public-holidays")]
public class PublicHolidaysController(IPublicHolidayService publicHolidayService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PublicHolidayDto>>> GetAll(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var holidays = fromDate.HasValue || toDate.HasValue
            ? await publicHolidayService.GetByDateRangeAsync(fromDate, toDate, cancellationToken)
            : await publicHolidayService.GetAllAsync(cancellationToken);

        var response = holidays.Select(x => new PublicHolidayDto
        {
            Id = x.Id,
            HolidayDate = x.HolidayDate,
            Name = x.Name,
            Source = x.Source,
            IsImported = x.IsImported,
            CreatedAt = x.CreatedAt,
        });

        return Ok(response);
    }

    [HttpPost("import")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<TaxExpenseTracker.Api.Models.PublicHolidayImportResultDto>> Import(
        IFormFile file,
        string? source,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("CSV file is required.", nameof(file));
        }

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var csv = await reader.ReadToEndAsync(cancellationToken);

        var result = await publicHolidayService.ImportAsync(csv, source, cancellationToken);

        return Ok(new TaxExpenseTracker.Api.Models.PublicHolidayImportResultDto
        {
            ImportedCount = result.ImportedCount,
            SkippedDuplicateCount = result.SkippedDuplicateCount,
            Warnings = result.Warnings.ToList(),
        });
    }
}
