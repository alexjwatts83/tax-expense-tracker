using Microsoft.AspNetCore.Mvc;
using TaxExpenseTracker.Api.Models;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.WorkFromHome;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/work-from-home")]
public class WorkFromHomeController(
    IWorkFromHomeService workFromHomeService,
    ILogger<WorkFromHomeController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkFromHomeDto>>> GetAll(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var entries = fromDate.HasValue || toDate.HasValue
            ? await workFromHomeService.GetByDateRangeAsync(fromDate, toDate, cancellationToken)
            : await workFromHomeService.GetAllAsync(cancellationToken);

        return Ok(entries.Select(MapWorkFromHome));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkFromHomeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entry = await workFromHomeService.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        return Ok(MapWorkFromHome(entry));
    }

    [HttpPost]
    public async Task<ActionResult<WorkFromHomeDto>> Create(CreateWorkFromHomeDto request, CancellationToken cancellationToken)
    {
        var entry = await workFromHomeService.CreateAsync(
            new CreateWorkFromHomeCommand(
                request.WorkDate,
                request.EntryType,
                request.SpecificHours,
                request.Notes),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, MapWorkFromHome(entry));
    }

    [HttpPost("batch")]
    public async Task<ActionResult<WorkFromHomeBatchResultDto>> BatchCreate(
        CreateWorkFromHomeBatchDto request,
        CancellationToken cancellationToken)
    {
        var requestedCount = request.Items?.Count ?? 0;
        logger.LogInformation("WFH batch create requested with {RequestedCount} items.", requestedCount);

        var commands = (request.Items ?? [])
            .Select(x => new CreateWorkFromHomeCommand(
                x.WorkDate,
                x.EntryType,
                x.SpecificHours,
                x.Notes))
            .ToList();

        var result = await workFromHomeService.BatchCreateAsync(commands, cancellationToken);

        logger.LogInformation(
            "WFH batch create completed. Requested={RequestedCount}, Created={CreatedCount}, Skipped={SkippedCount}, Failed={FailedCount}.",
            result.TotalRequested,
            result.CreatedCount,
            result.SkippedCount,
            result.FailedCount);

        if (result.FailedCount > 0)
        {
            logger.LogWarning("WFH batch create had failures for {FailedCount} items.", result.FailedCount);
        }

        return Ok(new WorkFromHomeBatchResultDto
        {
            TotalRequested = result.TotalRequested,
            CreatedCount = result.CreatedCount,
            SkippedCount = result.SkippedCount,
            FailedCount = result.FailedCount,
            Results = result.Results
                .Select(x => new WorkFromHomeBatchItemResultDto
                {
                    WorkDate = x.WorkDate,
                    EntryType = x.EntryType,
                    SpecificHours = x.SpecificHours,
                    Notes = x.Notes,
                    Status = x.Status,
                    Message = x.Message,
                    Entry = x.Entry is null ? null : MapWorkFromHome(x.Entry),
                })
                .ToList(),
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkFromHomeDto>> Update(Guid id, CreateWorkFromHomeDto request, CancellationToken cancellationToken)
    {
        var updated = await workFromHomeService.UpdateAsync(
            id,
            new UpdateWorkFromHomeCommand(
                request.WorkDate,
                request.EntryType,
                request.SpecificHours,
                request.Notes),
            cancellationToken);

        if (!updated)
        {
            return NotFound();
        }

        var entry = await workFromHomeService.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        return Ok(MapWorkFromHome(entry));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await workFromHomeService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var restored = await workFromHomeService.RestoreAsync(id, cancellationToken);
        if (!restored)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<ActionResult<WorkFromHomeSummaryDto>> Summary(
        string view = "month",
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var summaryView = ParseSummaryView(view);
        var anchorDate = date?.Date ?? DateTime.UtcNow.Date;

        var summary = await workFromHomeService.GetSummaryAsync(summaryView, anchorDate, cancellationToken);

        return Ok(new WorkFromHomeSummaryDto
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

    private static WorkFromHomeDto MapWorkFromHome(WorkFromHomeReadDto entry)
    {
        return new WorkFromHomeDto
        {
            Id = entry.Id,
            WorkDate = entry.WorkDate,
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