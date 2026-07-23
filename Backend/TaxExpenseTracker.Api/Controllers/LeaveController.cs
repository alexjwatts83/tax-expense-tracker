using Microsoft.AspNetCore.Mvc;
using TaxExpenseTracker.Api.Models;
using TaxExpenseTracker.Application.Leave;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/leave")]
public class LeaveController(ILeaveService leaveService) : ControllerBase
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
}