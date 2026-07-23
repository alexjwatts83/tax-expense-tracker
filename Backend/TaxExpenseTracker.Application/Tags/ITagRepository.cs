using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.Tags;

public interface ITagRepository : ISoftDeleteRepository<Tag>;
