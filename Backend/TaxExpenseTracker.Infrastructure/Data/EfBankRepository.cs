using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.Banks;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfBankRepository : EfSoftDeleteRepository<Bank>, IBankRepository
{
    public EfBankRepository(AppDbContext dbContext)
        : base(dbContext, dbContext.Banks)
    {
    }
}