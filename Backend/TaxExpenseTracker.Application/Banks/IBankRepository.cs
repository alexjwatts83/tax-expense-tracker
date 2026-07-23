using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.Banks;

public interface IBankRepository : ISoftDeleteRepository<Bank>;