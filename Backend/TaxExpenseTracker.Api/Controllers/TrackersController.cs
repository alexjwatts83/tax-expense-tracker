using Microsoft.AspNetCore.Mvc;
using TaxExpenseTracker.Api.Models;
using TaxExpenseTracker.Application.Trackers;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/trackers")]
public class TrackersController(ITrackerService trackerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrackerDto>>> GetAll(CancellationToken cancellationToken)
    {
        var trackers = await trackerService.GetAllAsync(cancellationToken);

        var response = trackers.Select(x => new TrackerDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            CreatedAt = x.CreatedAt
        });

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TrackerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tracker = await trackerService.GetByIdAsync(id, cancellationToken);

        if (tracker is null)
        {
            return NotFound();
        }

        var response = new TrackerDto
        {
            Id = tracker.Id,
            Name = tracker.Name,
            Description = tracker.Description,
            CreatedAt = tracker.CreatedAt
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<TrackerDto>> Create(CreateTrackerDto request, CancellationToken cancellationToken)
    {
        try
        {
            var tracker = await trackerService.CreateAsync(
                new CreateTrackerCommand(request.Name, request.Description),
                cancellationToken);

            var response = new TrackerDto
            {
                Id = tracker.Id,
                Name = tracker.Name,
                Description = tracker.Description,
                CreatedAt = tracker.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = tracker.Id }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TrackerDto>> Update(Guid id, CreateTrackerDto request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await trackerService.UpdateAsync(
                id,
                new UpdateTrackerCommand(request.Name, request.Description),
                cancellationToken);

            if (!updated)
            {
                return NotFound();
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        var tracker = await trackerService.GetByIdAsync(id, cancellationToken);
        if (tracker is null)
        {
            return NotFound();
        }

        var response = new TrackerDto
        {
            Id = tracker.Id,
            Name = tracker.Name,
            Description = tracker.Description,
            CreatedAt = tracker.CreatedAt
        };

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await trackerService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}