using Microsoft.AspNetCore.Mvc;
using TaxExpenseTracker.Api.Models;
using TaxExpenseTracker.Application.Tags;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController(ITagService tagService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TagDto>>> GetAll(CancellationToken cancellationToken)
    {
        var tags = await tagService.GetAllAsync(cancellationToken);

        var response = tags.Select(x => new TagDto
        {
            Id = x.Id,
            Name = x.Name,
            CreatedAt = x.CreatedAt
        });

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TagDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tag = await tagService.GetByIdAsync(id, cancellationToken);

        if (tag is null)
        {
            return NotFound();
        }

        var response = new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            CreatedAt = tag.CreatedAt
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<TagDto>> Create(CreateTagDto request, CancellationToken cancellationToken)
    {
        var tag = await tagService.CreateAsync(new CreateTagCommand(request.Name), cancellationToken);

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
        var updated = await tagService.UpdateAsync(id, new UpdateTagCommand(request.Name), cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        var tag = await tagService.GetByIdAsync(id, cancellationToken);
        if (tag is null)
        {
            return NotFound();
        }

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
        var deleted = await tagService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}