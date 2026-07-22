using Microsoft.AspNetCore.Mvc;
using TaxExpenseTracker.Api.Models;
using TaxExpenseTracker.Application.Banks;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/banks")]
public class BanksController(IBankService bankService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BankDto>>> GetAll(CancellationToken cancellationToken)
    {
        var banks = await bankService.GetAllAsync(cancellationToken);

        var response = banks.Select(x => new BankDto
        {
            Id = x.Id,
            Name = x.Name,
            CreatedAt = x.CreatedAt
        });

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BankDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var bank = await bankService.GetByIdAsync(id, cancellationToken);

        if (bank is null)
        {
            return NotFound();
        }

        var response = new BankDto
        {
            Id = bank.Id,
            Name = bank.Name,
            CreatedAt = bank.CreatedAt
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<BankDto>> Create(CreateBankDto request, CancellationToken cancellationToken)
    {
        var bank = await bankService.CreateAsync(new CreateBankCommand(request.Name), cancellationToken);

        var response = new BankDto
        {
            Id = bank.Id,
            Name = bank.Name,
            CreatedAt = bank.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = bank.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BankDto>> Update(Guid id, CreateBankDto request, CancellationToken cancellationToken)
    {
        var updated = await bankService.UpdateAsync(id, new UpdateBankCommand(request.Name), cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

        var bank = await bankService.GetByIdAsync(id, cancellationToken);
        if (bank is null)
        {
            return NotFound();
        }

        var response = new BankDto
        {
            Id = bank.Id,
            Name = bank.Name,
            CreatedAt = bank.CreatedAt
        };

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await bankService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var restored = await bankService.RestoreAsync(id, cancellationToken);
        if (!restored)
        {
            return NotFound();
        }

        return NoContent();
    }
}