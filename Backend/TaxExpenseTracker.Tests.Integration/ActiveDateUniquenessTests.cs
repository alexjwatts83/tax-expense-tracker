using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.Leave;
using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Infrastructure.Data;

namespace TaxExpenseTracker.Tests.Integration;

public class ActiveDateUniquenessTests
{
    [Fact]
    public void DuplicateActiveWorkLocationDates_AreRejectedByDatabase()
    {
        using var factory = new TaxExpenseTrackerApiFactory();
        using var _ = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var date = new DateOnly(2031, 1, 15);

        db.WorkLocationEntries.Add(WorkLocationEntry.Create(date, DayEntryType.FullDay, null, null, TimeProvider.System));
        db.SaveChanges();
        db.WorkLocationEntries.Add(WorkLocationEntry.Create(date, DayEntryType.HalfDay, null, null, TimeProvider.System));

        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void DuplicateActiveLeaveDates_AreRejectedByDatabase()
    {
        using var factory = new TaxExpenseTrackerApiFactory();
        using var _ = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var date = new DateOnly(2031, 2, 20);

        db.LeaveEntries.Add(LeaveEntry.Create(date, DayEntryType.FullDay, null, null, TimeProvider.System));
        db.SaveChanges();
        db.LeaveEntries.Add(LeaveEntry.Create(date, DayEntryType.HalfDay, null, null, TimeProvider.System));

        Assert.Throws<DbUpdateException>(() => db.SaveChanges());
    }

    [Fact]
    public void SoftDeletedEntry_DoesNotBlockActiveEntryOnSameDate()
    {
        using var factory = new TaxExpenseTrackerApiFactory();
        using var _ = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var date = new DateOnly(2031, 3, 25);

        var deleted = WorkLocationEntry.Create(date, DayEntryType.FullDay, null, null, TimeProvider.System);
        deleted.SoftDelete(TimeProvider.System);
        db.WorkLocationEntries.Add(deleted);
        db.SaveChanges();

        db.WorkLocationEntries.Add(WorkLocationEntry.Create(date, DayEntryType.HalfDay, null, null, TimeProvider.System));

        var saved = db.SaveChanges();

        Assert.Equal(1, saved);
    }

    [Fact]
    public async Task UniqueConstraintRace_ThroughRepository_IsTranslatedToConflict()
    {
        using var factory = new TaxExpenseTrackerApiFactory();
        using var _ = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILeaveRepository>();
        var date = new DateOnly(2032, 5, 5);

        await repository.AddAsync(LeaveEntry.Create(date, DayEntryType.FullDay, null, null, TimeProvider.System));
        await repository.SaveChangesAsync();
        await repository.AddAsync(LeaveEntry.Create(date, DayEntryType.HalfDay, null, null, TimeProvider.System));

        await Assert.ThrowsAsync<ApplicationConflictException>(() => repository.SaveChangesAsync());
    }
}
