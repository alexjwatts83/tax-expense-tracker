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
}
