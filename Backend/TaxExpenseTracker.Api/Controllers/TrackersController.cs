using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Api.Data;
using TaxExpenseTracker.Api.Models;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/trackers")]
public class TrackersController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrackerDto>>> GetAll(CancellationToken cancellationToken)
    {
        var trackers = await dbContext.Trackers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new TrackerDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(trackers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TrackerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tracker = await dbContext.Trackers
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TrackerDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tracker is null)
        {
            return NotFound();
        }

        return Ok(tracker);
    }

    [HttpPost]
    public async Task<ActionResult<TrackerDto>> Create(CreateTrackerDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Tracker name is required.");
        }

        var tracker = new Tracker
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        dbContext.Trackers.Add(tracker);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new TrackerDto
        {
            Id = tracker.Id,
            Name = tracker.Name,
            Description = tracker.Description,
            CreatedAt = tracker.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = tracker.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TrackerDto>> Update(Guid id, CreateTrackerDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Tracker name is required.");
        }

        var tracker = await dbContext.Trackers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tracker is null)
        {
            return NotFound();
        }

        tracker.Name = request.Name.Trim();
        tracker.Description = request.Description?.Trim();
        tracker.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

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
        var tracker = await dbContext.Trackers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tracker is null)
        {
            return NotFound();
        }

        tracker.IsDeleted = true;
        tracker.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}