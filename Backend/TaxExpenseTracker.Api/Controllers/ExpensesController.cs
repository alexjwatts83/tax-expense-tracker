using Microsoft.AspNetCore.Mvc;
using TaxExpenseTracker.Api.Models;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.Expenses;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/expenses")]
public class ExpensesController(IExpenseService expenseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ExpenseResponseDto>>> GetAll(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var expenses = await expenseService.GetAllAsync(page, pageSize, cancellationToken);
        var response = new PagedResult<ExpenseResponseDto>
        {
            Items = expenses.Items.Select(MapExpense).ToList(),
            TotalCount = expenses.TotalCount,
            PageNumber = expenses.PageNumber,
            PageSize = expenses.PageSize
        };

        return Ok(response);
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
        var expense = await expenseService.CreateAsync(
            new CreateExpenseCommand(
                request.Description,
                request.Date,
                request.BankId,
                request.Price,
                request.SourceId,
                request.TagIds),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, MapExpense(expense));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExpenseResponseDto>> Update(Guid id, CreateExpenseDto request, CancellationToken cancellationToken)
    {
        var expense = await expenseService.UpdateAsync(
            id,
            new UpdateExpenseCommand(
                request.Description,
                request.Date,
                request.BankId,
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

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var restored = await expenseService.RestoreAsync(id, cancellationToken);
        if (!restored)
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
        DateTime? date,
        Guid? bankId,
        decimal? price,
        Guid? sourceId,
        string? tagIds,
        CancellationToken cancellationToken)
    {
        var parsedTagIds = ParseTagIds(tagIds);

        var expenses = await expenseService.FilterAsync(
            new ExpenseFilterQuery(date, bankId, price, sourceId, parsedTagIds),
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
            Description = expense.Description,
            Date = expense.Date,
            BankId = expense.BankId,
            Bank = expense.Bank is null
                ? null
                : new BankDto
                {
                    Id = expense.Bank.Id,
                    Name = expense.Bank.Name,
                    CreatedAt = expense.Bank.CreatedAt
                },
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
                    Color = x.Color,
                    CreatedAt = x.CreatedAt
                })
                .ToList(),
            CreatedAt = expense.CreatedAt,
            UpdatedAt = expense.UpdatedAt
        };
    }
}
