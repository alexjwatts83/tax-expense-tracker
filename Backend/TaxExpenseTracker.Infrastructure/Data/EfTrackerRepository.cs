using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.Trackers;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfTrackerRepository : EfSoftDeleteRepository<Tracker>, ITrackerRepository
{
    public EfTrackerRepository(AppDbContext dbContext)
        : base(dbContext, dbContext.Trackers)
    {
    }
}
