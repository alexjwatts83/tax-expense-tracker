using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Api.Data;
using TaxExpenseTracker.Api.Models;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/expenses")]
public class ExpensesController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseResponseDto>>> GetAll(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var expenses = await dbContext.TaxExpenses
            .AsNoTracking()
            .Include(x => x.Source)
            .Include(x => x.TaxExpenseTags)
                .ThenInclude(x => x.Tag)
            .OrderByDescending(x => x.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(expenses.Select(MapExpense));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExpenseResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var expense = await dbContext.TaxExpenses
            .AsNoTracking()
            .Include(x => x.Source)
            .Include(x => x.TaxExpenseTags)
                .ThenInclude(x => x.Tag)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (expense is null)
        {
            return NotFound();
        }

        return Ok(MapExpense(expense));
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseResponseDto>> Create(CreateExpenseDto request, CancellationToken cancellationToken)
    {
        var sourceExists = await dbContext.Trackers.AnyAsync(x => x.Id == request.SourceId, cancellationToken);
        if (!sourceExists)
        {
            return BadRequest("Source tracker does not exist.");
        }

        var validTagIds = await dbContext.Tags
            .Where(x => request.TagIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        TaxExpense expense;
        try
        {
            expense = TaxExpense.Create(
                request.Item,
                request.Description,
                request.Date,
                request.Bank,
                request.Price,
                request.SourceId);

            expense.TaxExpenseTags = validTagIds
                .Select(tagId => TaxExpenseTag.Create(expense.Id, tagId))
                .ToList();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        dbContext.TaxExpenses.Add(expense);
        await dbContext.SaveChangesAsync(cancellationToken);

        var createdExpense = await dbContext.TaxExpenses
            .AsNoTracking()
            .Include(x => x.Source)
            .Include(x => x.TaxExpenseTags)
                .ThenInclude(x => x.Tag)
            .FirstAsync(x => x.Id == expense.Id, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, MapExpense(createdExpense));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExpenseResponseDto>> Update(Guid id, CreateExpenseDto request, CancellationToken cancellationToken)
    {
        var expense = await dbContext.TaxExpenses
            .Include(x => x.TaxExpenseTags)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (expense is null)
        {
            return NotFound();
        }

        var sourceExists = await dbContext.Trackers.AnyAsync(x => x.Id == request.SourceId, cancellationToken);
        if (!sourceExists)
        {
            return BadRequest("Source tracker does not exist.");
        }

        var validTagIds = await dbContext.Tags
            .Where(x => request.TagIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        try
        {
            expense.UpdateDetails(
                request.Item,
                request.Description,
                request.Date,
                request.Bank,
                request.Price,
                request.SourceId);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        expense.TaxExpenseTags.Clear();
        foreach (var tagId in validTagIds)
        {
            expense.TaxExpenseTags.Add(TaxExpenseTag.Create(expense.Id, tagId));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var updatedExpense = await dbContext.TaxExpenses
            .AsNoTracking()
            .Include(x => x.Source)
            .Include(x => x.TaxExpenseTags)
                .ThenInclude(x => x.Tag)
            .FirstAsync(x => x.Id == id, cancellationToken);

        return Ok(MapExpense(updatedExpense));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        var expense = await dbContext.TaxExpenses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (expense is null)
        {
            return NotFound();
        }

        expense.SoftDelete();
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var totalSpent = await dbContext.TaxExpenses.SumAsync(x => x.Price, cancellationToken);

        var byBank = await dbContext.TaxExpenses
            .GroupBy(x => x.Bank)
            .Select(x => new { Bank = x.Key, Total = x.Sum(e => e.Price) })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken);

        var bySource = await dbContext.TaxExpenses
            .Include(x => x.Source)
            .GroupBy(x => x.Source!.Name)
            .Select(x => new { Source = x.Key, Total = x.Sum(e => e.Price) })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            totalSpent,
            byBank,
            bySource
        });
    }

    [HttpGet("filter")]
    public async Task<ActionResult<IEnumerable<ExpenseResponseDto>>> Filter(
        DateTime? startDate,
        DateTime? endDate,
        string? bank,
        decimal? minPrice,
        decimal? maxPrice,
        Guid? sourceId,
        string? tagIds,
        CancellationToken cancellationToken)
    {
        var query = dbContext.TaxExpenses
            .AsNoTracking()
            .Include(x => x.Source)
            .Include(x => x.TaxExpenseTags)
                .ThenInclude(x => x.Tag)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(x => x.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.Date <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(bank))
        {
            var trimmedBank = bank.Trim();
            query = query.Where(x => x.Bank == trimmedBank);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(x => x.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x => x.Price <= maxPrice.Value);
        }

        if (sourceId.HasValue)
        {
            query = query.Where(x => x.SourceId == sourceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(tagIds))
        {
            var parsedTagIds = tagIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => Guid.TryParse(value, out var tagId) ? tagId : Guid.Empty)
                .Where(tagId => tagId != Guid.Empty)
                .ToList();

            if (parsedTagIds.Count > 0)
            {
                query = query.Where(x => x.TaxExpenseTags.Any(tag => parsedTagIds.Contains(tag.TagId)));
            }
        }

        var expenses = await query
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);

        return Ok(expenses.Select(MapExpense));
    }

    private static ExpenseResponseDto MapExpense(TaxExpense expense)
    {
        return new ExpenseResponseDto
        {
            Id = expense.Id,
            Item = expense.Item,
            Description = expense.Description,
            Date = expense.Date,
            Bank = expense.Bank,
            Price = expense.Price,
            SourceId = expense.SourceId,
            Source = expense.Source is null
                ? null
                : new TrackerDto
                {
                    Id = expense.Source.Id,
                    Name = expense.Source.Name,
                    Description = expense.Source.Description,
                    CreatedAt = expense.Source.CreatedAt
                },
            Tags = expense.TaxExpenseTags
                .Where(x => x.Tag is not null)
                .Select(x => new TagDto
                {
                    Id = x.Tag!.Id,
                    Name = x.Tag.Name,
                    CreatedAt = x.Tag.CreatedAt
                })
                .ToList(),
            CreatedAt = expense.CreatedAt,
            UpdatedAt = expense.UpdatedAt
        };
    }
}