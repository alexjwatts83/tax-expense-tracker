using Microsoft.AspNetCore.Mvc;
using TaxExpenseTracker.Api.Models;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.WorkLocation;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/work-locations")]
[Route("api/work-from-home")]
public class WorkLocationController(
    IWorkLocationService workLocationService,
    ILogger<WorkLocationController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkLocationDto>>> GetAll(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var entries = fromDate.HasValue || toDate.HasValue
            ? await workLocationService.GetByDateRangeAsync(fromDate, toDate, cancellationToken)
            : await workLocationService.GetAllAsync(cancellationToken);

        return Ok(entries.Select(MapWorkLocation));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkLocationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entry = await workLocationService.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        return Ok(MapWorkLocation(entry));
    }

    [HttpPost]
    public async Task<ActionResult<WorkLocationDto>> Create(CreateWorkLocationDto request, CancellationToken cancellationToken)
    {
        var entry = await workLocationService.CreateAsync(
            new CreateWorkLocationCommand(
                request.WorkDate,
                request.EntryType,
                request.SpecificHours,
                request.Notes,
                request.WorkLocation),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, MapWorkLocation(entry));
    }

    [HttpPost("batch")]
    public async Task<ActionResult<WorkLocationBatchResultDto>> BatchCreate(
        CreateWorkLocationBatchDto request,
        CancellationToken cancellationToken)
    {
        var requestedCount = request.Items?.Count ?? 0;
        logger.LogInformation("Work-location batch create requested with {RequestedCount} items.", requestedCount);

        var commands = (request.Items ?? [])
            .Select(x => new CreateWorkLocationCommand(
                x.WorkDate,
                x.EntryType,
                x.SpecificHours,
                x.Notes,
                x.WorkLocation))
            .ToList();

        var result = await workLocationService.BatchCreateAsync(commands, cancellationToken);

        logger.LogInformation(
            "Work-location batch create completed. Requested={RequestedCount}, Created={CreatedCount}, Skipped={SkippedCount}, Failed={FailedCount}.",
            result.TotalRequested,
            result.CreatedCount,
            result.SkippedCount,
            result.FailedCount);

        if (result.FailedCount > 0)
        {
            logger.LogWarning("Work-location batch create had failures for {FailedCount} items.", result.FailedCount);
        }

        return Ok(new WorkLocationBatchResultDto
        {
            TotalRequested = result.TotalRequested,
            CreatedCount = result.CreatedCount,
            SkippedCount = result.SkippedCount,
            FailedCount = result.FailedCount,
            Results = result.Results
                .Select(x => new WorkLocationBatchItemResultDto
                {
                    WorkDate = x.WorkDate,
                    WorkLocation = x.WorkLocation,
                    EntryType = x.EntryType,
                    SpecificHours = x.SpecificHours,
                    Notes = x.Notes,
                    Status = x.Status,
                    Message = x.Message,
                    Entry = x.Entry is null ? null : MapWorkLocation(x.Entry),
                })
                .ToList(),
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkLocationDto>> Update(Guid id, CreateWorkLocationDto request, CancellationToken cancellationToken)
    {
        var updated = await workLocationService.UpdateAsync(
            id,
            new UpdateWorkLocationCommand(
                request.WorkDate,
                request.EntryType,
                request.SpecificHours,
                request.Notes,
                request.WorkLocation),
            cancellationToken);

        if (!updated)
        {
            return NotFound();
        }

        var entry = await workLocationService.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        return Ok(MapWorkLocation(entry));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await workLocationService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var restored = await workLocationService.RestoreAsync(id, cancellationToken);
        if (!restored)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<ActionResult<WorkLocationSummaryDto>> Summary(
        string view = "month",
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var summaryView = ParseSummaryView(view);
        var anchorDate = date?.Date ?? DateTime.UtcNow.Date;

        var summary = await workLocationService.GetSummaryAsync(summaryView, anchorDate, cancellationToken);

        return Ok(new WorkLocationSummaryDto
        {
            View = view.ToLowerInvariant(),
            AnchorDate = anchorDate,
            FromDate = summary.FromDate,
            ToDate = summary.ToDate,
            TotalHours = summary.TotalHours,
            TotalDays = summary.TotalDays,
            EntryCount = summary.EntryCount,
            Holidays = summary.Holidays
                .Select(x => new SummaryHolidayDto
                {
                    Date = x.Date,
                    Name = x.Name,
                })
                .ToList(),
        });
    }

    private static WorkLocationDto MapWorkLocation(WorkLocationReadDto entry)
    {
        return new WorkLocationDto
        {
            Id = entry.Id,
            WorkDate = entry.WorkDate,
            WorkLocation = entry.WorkLocation,
            EntryType = entry.EntryType,
            HoursWorked = entry.HoursWorked,
            Notes = entry.Notes,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };
    }

    private static SummaryView ParseSummaryView(string view)
    {
        if (string.Equals(view, "week", StringComparison.OrdinalIgnoreCase))
        {
            return SummaryView.Week;
        }

        if (string.Equals(view, "month", StringComparison.OrdinalIgnoreCase))
        {
            return SummaryView.Month;
        }

        throw new ArgumentException("view must be 'week' or 'month'.", nameof(view));
    }
}