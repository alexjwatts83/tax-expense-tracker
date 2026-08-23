using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using TaxExpenseTracker.Application.Banks;
using TaxExpenseTracker.Application.Expenses;
using TaxExpenseTracker.Application.Leave;
using TaxExpenseTracker.Application.PublicHolidays;
using TaxExpenseTracker.Application.Tags;
using TaxExpenseTracker.Application.Trackers;
using TaxExpenseTracker.Application.WorkLocation;
using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Infrastructure.Data;

namespace TaxExpenseTracker.Tests.Integration;

public class PersistenceModelInvariantTests
{
    [Fact]
    public void SoftDeleteEntities_HaveExpectedQueryFilters()
    {
        using var context = CreateContext();
        var filteredTypes = new[]
        {
            typeof(TaxExpense),
            typeof(WorkLocationEntry),
            typeof(LeaveEntry),
            typeof(Bank),
            typeof(Tracker),
            typeof(Tag),
            typeof(TaxExpenseTag),
        };

        Assert.All(filteredTypes, type => Assert.NotEmpty(context.Model.FindEntityType(type)!.GetDeclaredQueryFilters()));
        Assert.Empty(context.Model.FindEntityType(typeof(PublicHoliday))!.GetDeclaredQueryFilters());
    }

    [Fact]
    public void RelationshipsAndUniqueIndexes_EnforcePersistenceInvariants()
    {
        using var context = CreateContext();

        AssertUniqueIndex(context, typeof(WorkLocationEntry), nameof(WorkLocationEntry.WorkDate));
        AssertUniqueIndex(context, typeof(LeaveEntry), nameof(LeaveEntry.LeaveDate));
        AssertUniqueIndex(context, typeof(PublicHoliday), nameof(PublicHoliday.HolidayDate), nameof(PublicHoliday.Name));
        AssertUniqueIndex(context, typeof(TaxExpenseTag), nameof(TaxExpenseTag.TaxExpenseId), nameof(TaxExpenseTag.TagId));

        AssertForeignKey<TaxExpense, Bank>(context, nameof(TaxExpense.BankId), DeleteBehavior.Restrict);
        AssertForeignKey<TaxExpense, Tracker>(context, nameof(TaxExpense.SourceId), DeleteBehavior.Restrict);
        AssertForeignKey<TaxExpenseTag, TaxExpense>(context, nameof(TaxExpenseTag.TaxExpenseId), DeleteBehavior.Cascade);
        AssertForeignKey<TaxExpenseTag, Tag>(context, nameof(TaxExpenseTag.TagId), DeleteBehavior.Cascade);
    }

    [Fact]
    public void RepositoryInterfaces_ResolveExpectedEfImplementations()
    {
        using var factory = new TaxExpenseTrackerApiFactory();
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        Assert.IsType<EfTrackerRepository>(services.GetRequiredService<ITrackerRepository>());
        Assert.IsType<EfTagRepository>(services.GetRequiredService<ITagRepository>());
        Assert.IsType<EfBankRepository>(services.GetRequiredService<IBankRepository>());
        Assert.IsType<EfExpenseRepository>(services.GetRequiredService<IExpenseRepository>());
        Assert.IsType<EfWorkLocationRepository>(services.GetRequiredService<IWorkLocationRepository>());
        Assert.IsType<EfLeaveRepository>(services.GetRequiredService<ILeaveRepository>());
        Assert.IsType<EfPublicHolidayRepository>(services.GetRequiredService<IPublicHolidayRepository>());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        return new AppDbContext(options);
    }

    private static void AssertUniqueIndex(AppDbContext context, Type entityType, params string[] propertyNames)
    {
        var index = context.Model.FindEntityType(entityType)!.GetIndexes()
            .Single(candidate => candidate.Properties.Select(property => property.Name).SequenceEqual(propertyNames));

        Assert.True(index.IsUnique);
    }

    private static void AssertForeignKey<TEntity, TPrincipal>(
        AppDbContext context,
        string propertyName,
        DeleteBehavior deleteBehavior)
    {
        var foreignKey = context.Model.FindEntityType(typeof(TEntity))!.GetForeignKeys()
            .Single(candidate => candidate.Properties.Single().Name == propertyName);

        Assert.Equal(typeof(TPrincipal), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(deleteBehavior, foreignKey.DeleteBehavior);
        Assert.True(foreignKey.IsRequired);
    }
}