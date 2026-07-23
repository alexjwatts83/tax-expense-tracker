using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.Trackers;

public interface ITrackerRepository : ISoftDeleteRepository<Tracker>;