using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Api.Data;
using TaxExpenseTracker.Api.Models;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TagDto>>> GetAll(CancellationToken cancellationToken)
    {
        var tags = await dbContext.Tags
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new TagDto
            {
                Id = x.Id,
                Name = x.Name,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(tags);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TagDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tag = await dbContext.Tags
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TagDto
            {
                Id = x.Id,
                Name = x.Name,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tag is null)
        {
            return NotFound();
        }

        return Ok(tag);
    }

    [HttpPost]
    public async Task<ActionResult<TagDto>> Create(CreateTagDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Tag name is required.");
        }

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        dbContext.Tags.Add(tag);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            CreatedAt = tag.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = tag.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TagDto>> Update(Guid id, CreateTagDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Tag name is required.");
        }

        var tag = await dbContext.Tags.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tag is null)
        {
            return NotFound();
        }

        tag.Name = request.Name.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            CreatedAt = tag.CreatedAt
        };

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        var tag = await dbContext.Tags.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tag is null)
        {
            return NotFound();
        }

        tag.IsDeleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}