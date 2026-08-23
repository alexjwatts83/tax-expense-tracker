using Microsoft.Extensions.Time.Testing;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class DomainEntityDefaultsTests
{
    [Fact]
    public void Tracker_Defaults_AreInitialized()
    {
        var tracker = new Tracker();

        Assert.Equal(string.Empty, tracker.Name);
        Assert.NotNull(tracker.Expenses);
        Assert.Empty(tracker.Expenses);
    }

    [Fact]
    public void TaxExpense_Defaults_AreInitialized()
    {
        var expense = new TaxExpense();

        Assert.Equal(string.Empty, expense.Description);
        Assert.Equal(Guid.Empty, expense.BankId);
        Assert.NotNull(expense.TaxExpenseTags);
        Assert.Empty(expense.TaxExpenseTags);
    }

    [Fact]
    public void Tracker_Create_Throws_WhenNameMissing()
    {
        Assert.Throws<ArgumentException>(() => Tracker.Create("   ", null, TestTime.TimeProvider));
    }

    [Fact]
    public void Tag_Create_Throws_WhenNameMissing()
    {
        Assert.Throws<ArgumentException>(() => Tag.Create("", null, TestTime.TimeProvider));
    }

    [Fact]
    public void TaxExpense_Create_Throws_WhenPriceNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TaxExpense.Create("Desc", DateOnly.FromDateTime(TestTime.FixedUtcNow.UtcDateTime), Guid.NewGuid(), -1m, Guid.NewGuid(), TestTime.TimeProvider));
    }

    [Fact]
    public void TaxExpense_Create_Throws_WhenDateDefault()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TaxExpense.Create("Desc", default, Guid.NewGuid(), 1m, Guid.NewGuid(), TestTime.TimeProvider));
    }

    [Fact]
    public void LeaveEntry_Create_Defaults_ToAnnualLeaveType()
    {
        var entry = LeaveEntry.Create(new DateOnly(2026, 3, 10), DayEntryType.FullDay, null, null, TestTime.TimeProvider);

        Assert.Equal(LeaveType.Annual, entry.LeaveType);
    }

    [Fact]
    public void LeaveEntry_Create_Throws_WhenLeaveTypeInvalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LeaveEntry.Create(new DateOnly(2026, 3, 10), DayEntryType.FullDay, null, null, TestTime.TimeProvider, (LeaveType)99));
    }

    [Fact]
    public void TimeEntry_StateSetters_AreNotPublic()
    {
        var leaveProperties = new[] { "LeaveDate", "LeaveType", "EntryType", "HoursWorked", "Notes" };
        var workProperties = new[] { "WorkDate", "WorkLocation", "EntryType", "HoursWorked", "Notes" };

        Assert.All(leaveProperties, name => Assert.False(typeof(LeaveEntry).GetProperty(name)!.SetMethod!.IsPublic));
        Assert.All(workProperties, name => Assert.False(typeof(WorkLocationEntry).GetProperty(name)!.SetMethod!.IsPublic));
    }

    [Fact]
    public void TimeEntry_Import_PreservesProvidedIdentity()
    {
        var leaveId = Guid.NewGuid();
        var workId = Guid.NewGuid();

        var leave = LeaveEntry.Import(leaveId, new DateOnly(2026, 3, 11), DayEntryType.HalfDay, null, " Leave ", TestTime.TimeProvider, LeaveType.Sick);
        var work = WorkLocationEntry.Import(workId, new DateOnly(2026, 3, 12), DayEntryType.SpecificHours, 4.5m, " Work ", TestTime.TimeProvider, WorkLocationType.Office);

        Assert.Equal(leaveId, leave.Id);
        Assert.Equal(LeaveType.Sick, leave.LeaveType);
        Assert.Equal("Leave", leave.Notes);
        Assert.Equal(workId, work.Id);
        Assert.Equal(WorkLocationType.Office, work.WorkLocation);
        Assert.Equal(4.5m, work.HoursWorked);
        Assert.Equal("Work", work.Notes);
    }

    [Fact]
    public void TimeEntries_Create_RejectInvalidHoursAndEntryTypes()
    {
        var date = new DateOnly(2026, 3, 13);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LeaveEntry.Create(date, DayEntryType.SpecificHours, 0m, null, TestTime.TimeProvider));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WorkLocationEntry.Create(date, DayEntryType.SpecificHours, 24.1m, null, TestTime.TimeProvider));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LeaveEntry.Create(date, (DayEntryType)99, null, null, TestTime.TimeProvider));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WorkLocationEntry.Create(date, (DayEntryType)99, null, null, TestTime.TimeProvider));
    }

    [Fact]
    public void TimeEntries_Update_InvalidHours_DoesNotMutateState()
    {
        var originalDate = new DateOnly(2026, 3, 14);
        var leave = LeaveEntry.Create(originalDate, DayEntryType.FullDay, null, "Leave", TestTime.TimeProvider);
        var work = WorkLocationEntry.Create(originalDate, DayEntryType.HalfDay, null, "Work", TestTime.TimeProvider);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            leave.Update(originalDate.AddDays(1), DayEntryType.SpecificHours, null, "Changed", TestTime.TimeProvider, LeaveType.Sick));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            work.Update(originalDate.AddDays(1), DayEntryType.SpecificHours, 25m, "Changed", TestTime.TimeProvider, WorkLocationType.Office));

        Assert.Equal(originalDate, leave.LeaveDate);
        Assert.Equal(LeaveType.Annual, leave.LeaveType);
        Assert.Equal(DayEntryType.FullDay, leave.EntryType);
        Assert.Equal("Leave", leave.Notes);
        Assert.Equal(originalDate, work.WorkDate);
        Assert.Equal(WorkLocationType.Wfh, work.WorkLocation);
        Assert.Equal(DayEntryType.HalfDay, work.EntryType);
        Assert.Equal("Work", work.Notes);
    }

    [Fact]
    public void ReferenceEntity_StateSetters_AreNotPublic()
    {
        var trackerProperties = new[] { "Name", "Description", "Expenses", "CreatedAt", "UpdatedAt", "IsDeleted" };
        var tagProperties = new[] { "Name", "Color", "TaxExpenseTags", "CreatedAt", "UpdatedAt", "IsDeleted" };
        var bankProperties = new[] { "Name", "Expenses", "CreatedAt", "UpdatedAt", "IsDeleted" };
        var holidayProperties = new[] { "HolidayDate", "Name", "Source", "IsImported", "CanBeWorkedOn", "CreatedAt", "UpdatedAt" };

        Assert.All(trackerProperties, name => Assert.False(typeof(Tracker).GetProperty(name)!.SetMethod!.IsPublic));
        Assert.All(tagProperties, name => Assert.False(typeof(Tag).GetProperty(name)!.SetMethod!.IsPublic));
        Assert.All(bankProperties, name => Assert.False(typeof(Bank).GetProperty(name)!.SetMethod!.IsPublic));
        Assert.All(holidayProperties, name => Assert.False(typeof(PublicHoliday).GetProperty(name)!.SetMethod!.IsPublic));
    }

    [Fact]
    public void ReferenceEntity_Import_PreservesIdentityAndAuditTimestamps()
    {
        var createdAt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var updatedAt = createdAt.AddDays(1);
        var tracker = Tracker.Import(Guid.NewGuid(), "Tracker", null, TestTime.TimeProvider, createdAt, updatedAt);
        var tag = Tag.Import(Guid.NewGuid(), "Tag", "#123456", TestTime.TimeProvider, createdAt, updatedAt);
        var bank = Bank.Import(Guid.NewGuid(), "Bank", TestTime.TimeProvider, createdAt, updatedAt);
        var holiday = PublicHoliday.Import(Guid.NewGuid(), new DateOnly(2026, 1, 1), "Holiday", "Import", true, TestTime.TimeProvider, createdAt: createdAt, updatedAt: updatedAt);

        Assert.All(new AuditableEntity[] { tracker, tag, bank, holiday }, entity =>
        {
            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.Equal(createdAt, entity.CreatedAt);
            Assert.Equal(updatedAt, entity.UpdatedAt);
        });
    }

    [Fact]
    public void ReferenceEntity_Mutations_UpdateTimestampFromClock()
    {
        var timeProvider = new FakeTimeProvider(TestTime.FixedUtcNow);
        var tracker = Tracker.Create("Tracker", null, timeProvider);
        var tag = Tag.Create("Tag", null, timeProvider);
        var bank = Bank.Create("Bank", timeProvider);
        var holiday = PublicHoliday.Create(new DateOnly(2026, 1, 1), "Holiday", null, false, timeProvider);
        timeProvider.Advance(TimeSpan.FromHours(1));
        var expectedUpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

        tracker.Rename("Updated tracker", null, timeProvider);
        tag.Update("Updated tag", "#123456", timeProvider);
        bank.Rename("Updated bank", timeProvider);
        holiday.Update(new DateOnly(2026, 1, 2), "Updated holiday", "Manual", false, true, timeProvider);

        Assert.All(new AuditableEntity[] { tracker, tag, bank, holiday }, entity => Assert.Equal(expectedUpdatedAt, entity.UpdatedAt));
    }
}
