using Microsoft.AspNetCore.Mvc;
using TaxExpenseTracker.Api.Models;
using TaxExpenseTracker.Application.Expenses;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/expenses")]
public class ExpensesController(IExpenseService expenseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseResponseDto>>> GetAll(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var expenses = await expenseService.GetAllAsync(page, pageSize, cancellationToken);
        return Ok(expenses.Select(MapExpense));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExpenseResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var expense = await expenseService.GetByIdAsync(id, cancellationToken);
        if (expense is null)
        {
            return NotFound();
        }

        return Ok(MapExpense(expense));
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseResponseDto>> Create(CreateExpenseDto request, CancellationToken cancellationToken)
    {
        try
        {
            var expense = await expenseService.CreateAsync(
                new CreateExpenseCommand(
                    request.Item,
                    request.Description,
                    request.Date,
                    request.Bank,
                    request.Price,
                    request.SourceId,
                    request.TagIds),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = expense.Id }, MapExpense(expense));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExpenseResponseDto>> Update(Guid id, CreateExpenseDto request, CancellationToken cancellationToken)
    {
        try
        {
            var expense = await expenseService.UpdateAsync(
                id,
                new UpdateExpenseCommand(
                    request.Item,
                    request.Description,
                    request.Date,
                    request.Bank,
                    request.Price,
                    request.SourceId,
                    request.TagIds),
                cancellationToken);

            if (expense is null)
            {
                return NotFound();
            }

            return Ok(MapExpense(expense));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await expenseService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var summary = await expenseService.GetSummaryAsync(cancellationToken);

        return Ok(new
        {
            totalSpent = summary.TotalSpent,
            byBank = summary.ByBank.Select(x => new { Bank = x.Group, Total = x.Total }),
            bySource = summary.BySource.Select(x => new { Source = x.Group, Total = x.Total })
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
        var parsedTagIds = ParseTagIds(tagIds);

        var expenses = await expenseService.FilterAsync(
            new ExpenseFilterQuery(startDate, endDate, bank, minPrice, maxPrice, sourceId, parsedTagIds),
            cancellationToken);

        return Ok(expenses.Select(MapExpense));
    }

    private static List<Guid> ParseTagIds(string? tagIds)
    {
        if (string.IsNullOrWhiteSpace(tagIds))
        {
            return [];
        }

        return tagIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Guid.TryParse(value, out var tagId) ? tagId : Guid.Empty)
            .Where(tagId => tagId != Guid.Empty)
            .ToList();
    }

    private static ExpenseResponseDto MapExpense(ExpenseReadDto expense)
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
            Tags = expense.Tags
                .Select(x => new TagDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    CreatedAt = x.CreatedAt
                })
                .ToList(),
            CreatedAt = expense.CreatedAt,
            UpdatedAt = expense.UpdatedAt
        };
    }
}
