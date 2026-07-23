using Microsoft.AspNetCore.Mvc;
using TaxExpenseTracker.Api.Models;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.Leave;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/leave")]
public class LeaveController(
    ILeaveService leaveService,
    ILogger<LeaveController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeaveDto>>> GetAll(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var entries = fromDate.HasValue || toDate.HasValue
            ? await leaveService.GetByDateRangeAsync(fromDate, toDate, cancellationToken)
            : await leaveService.GetAllAsync(cancellationToken);

        return Ok(entries.Select(MapLeave));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaveDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entry = await leaveService.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        return Ok(MapLeave(entry));
    }

    [HttpPost]
    public async Task<ActionResult<LeaveDto>> Create(CreateLeaveDto request, CancellationToken cancellationToken)
    {
        var entry = await leaveService.CreateAsync(
            new CreateLeaveCommand(
                request.LeaveDate,
                request.EntryType,
                request.SpecificHours,
                request.Notes),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, MapLeave(entry));
    }

    [HttpPost("batch")]
    public async Task<ActionResult<LeaveBatchResultDto>> BatchCreate(
        CreateLeaveBatchDto request,
        CancellationToken cancellationToken)
    {
        var requestedCount = request.Items?.Count ?? 0;
        logger.LogInformation("Leave batch create requested with {RequestedCount} items.", requestedCount);

        var commands = (request.Items ?? [])
            .Select(x => new CreateLeaveCommand(
                x.LeaveDate,
                x.EntryType,
                x.SpecificHours,
                x.Notes))
            .ToList();

        var result = await leaveService.BatchCreateAsync(commands, cancellationToken);

        logger.LogInformation(
            "Leave batch create completed. Requested={RequestedCount}, Created={CreatedCount}, Skipped={SkippedCount}, Failed={FailedCount}.",
            result.TotalRequested,
            result.CreatedCount,
            result.SkippedCount,
            result.FailedCount);

        if (result.FailedCount > 0)
        {
            logger.LogWarning("Leave batch create had failures for {FailedCount} items.", result.FailedCount);
        }

        return Ok(new LeaveBatchResultDto
        {
            TotalRequested = result.TotalRequested,
            CreatedCount = result.CreatedCount,
            SkippedCount = result.SkippedCount,
            FailedCount = result.FailedCount,
            Results = result.Results
                .Select(x => new LeaveBatchItemResultDto
                {
                    LeaveDate = x.LeaveDate,
                    EntryType = x.EntryType,
                    SpecificHours = x.SpecificHours,
                    Notes = x.Notes,
                    Status = x.Status,
                    Message = x.Message,
                    Entry = x.Entry is null ? null : MapLeave(x.Entry),
                })
                .ToList(),
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LeaveDto>> Update(Guid id, CreateLeaveDto request, CancellationToken cancellationToken)
    {
        var updated = await leaveService.UpdateAsync(
            id,
            new UpdateLeaveCommand(
                request.LeaveDate,
                request.EntryType,
                request.SpecificHours,
                request.Notes),
            cancellationToken);

        if (!updated)
        {
            return NotFound();
        }

        var entry = await leaveService.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        return Ok(MapLeave(entry));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await leaveService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var restored = await leaveService.RestoreAsync(id, cancellationToken);
        if (!restored)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<ActionResult<LeaveSummaryDto>> Summary(
        string view = "month",
        DateTime? date = null,
        CancellationToken cancellationToken = default)
    {
        var summaryView = ParseSummaryView(view);
        var anchorDate = date?.Date ?? DateTime.UtcNow.Date;

        var summary = await leaveService.GetSummaryAsync(summaryView, anchorDate, cancellationToken);

        return Ok(new LeaveSummaryDto
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

    private static LeaveDto MapLeave(LeaveReadDto entry)
    {
        return new LeaveDto
        {
            Id = entry.Id,
            LeaveDate = entry.LeaveDate,
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