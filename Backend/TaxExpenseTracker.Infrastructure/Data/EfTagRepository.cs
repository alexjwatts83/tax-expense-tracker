using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.Tags;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfTagRepository : EfSoftDeleteRepository<Tag>, ITagRepository
{
    public EfTagRepository(AppDbContext dbContext)
        : base(dbContext, dbContext.Tags)
    {
    }
}
