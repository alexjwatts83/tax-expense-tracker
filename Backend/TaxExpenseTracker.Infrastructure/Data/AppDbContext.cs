using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaxExpense> TaxExpenses => Set<TaxExpense>();
    public DbSet<WorkLocationEntry> WorkLocationEntries => Set<WorkLocationEntry>();
    public DbSet<LeaveEntry> LeaveEntries => Set<LeaveEntry>();
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<Tracker> Trackers => Set<Tracker>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TaxExpenseTag> TaxExpenseTags => Set<TaxExpenseTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaxExpense>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<WorkLocationEntry>()
            .ToTable("WorkFromHomeEntries")
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<LeaveEntry>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Bank>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Tracker>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Tag>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<TaxExpenseTag>().HasQueryFilter(x => !x.TaxExpense!.IsDeleted && !x.Tag!.IsDeleted);

        modelBuilder.Entity<PublicHoliday>()
            .HasIndex(x => new { x.HolidayDate, x.Name })
            .IsUnique();

        modelBuilder.Entity<TaxExpense>()
            .HasOne(x => x.Bank)
            .WithMany(x => x.Expenses)
            .HasForeignKey(x => x.BankId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaxExpense>()
            .HasOne(x => x.Source)
            .WithMany(x => x.Expenses)
            .HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaxExpense>()
            .Property(x => x.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<TaxExpenseTag>()
            .HasIndex(x => new { x.TaxExpenseId, x.TagId })
            .IsUnique();

        modelBuilder.Entity<TaxExpenseTag>()
            .HasOne(x => x.TaxExpense)
            .WithMany(x => x.TaxExpenseTags)
            .HasForeignKey(x => x.TaxExpenseId);

        modelBuilder.Entity<TaxExpenseTag>()
            .HasOne(x => x.Tag)
            .WithMany(x => x.TaxExpenseTags)
            .HasForeignKey(x => x.TagId);

        var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Bank>().HasData(
            new Bank { Id = Guid.Parse("f2c328b0-6d89-4b66-8ef4-fcbe9970a1fd"), Name = "ANZ", IsDeleted = false, CreatedAt = seedTimestamp },
            new Bank { Id = Guid.Parse("4c52ddd3-5208-4385-bf85-c1d3e0402ef4"), Name = "CBA", IsDeleted = false, CreatedAt = seedTimestamp },
            new Bank { Id = Guid.Parse("69c4e618-d714-4429-b5a2-3a35eb50b343"), Name = "Westpac", IsDeleted = false, CreatedAt = seedTimestamp }
        );

        modelBuilder.Entity<Tracker>().HasData(
            new Tracker { Id = Guid.Parse("e13e64d9-bf27-4232-9af4-b2db537d5faf"), Name = "H&R Block", Description = "Default tracker", IsDeleted = false, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
            new Tracker { Id = Guid.Parse("03f8e309-dff6-4fd8-b1d8-280e7285687e"), Name = "Pluralsight", Description = "Default tracker", IsDeleted = false, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
            new Tracker { Id = Guid.Parse("8c8208af-c8d8-40f5-99cc-152f6f2f5fb6"), Name = "Udemy", Description = "Default tracker", IsDeleted = false, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
            new Tracker { Id = Guid.Parse("5288117d-0f1f-49ae-9f5f-4f17f8e7d7fb"), Name = "JB Hifi", Description = "Default tracker", IsDeleted = false, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
            new Tracker { Id = Guid.Parse("2eec1c4c-3e14-4375-b6ec-c3e696a84734"), Name = "Office Works", Description = "Default tracker", IsDeleted = false, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp }
        );

        modelBuilder.Entity<PublicHoliday>().HasData(
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165001"), Name = "New Year's Day", HolidayDate = new DateTime(2026, 1, 1), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165002"), Name = "New Year's Day", HolidayDate = new DateTime(2027, 1, 1), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165003"), Name = "Australia Day", HolidayDate = new DateTime(2026, 1, 26), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165004"), Name = "Australia Day", HolidayDate = new DateTime(2027, 1, 26), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165005"), Name = "Good Friday", HolidayDate = new DateTime(2026, 4, 3), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165006"), Name = "Good Friday", HolidayDate = new DateTime(2027, 3, 26), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165007"), Name = "Easter Saturday", HolidayDate = new DateTime(2026, 4, 4), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165008"), Name = "Easter Saturday", HolidayDate = new DateTime(2027, 3, 27), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165009"), Name = "Easter Sunday", HolidayDate = new DateTime(2026, 4, 5), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165010"), Name = "Easter Sunday", HolidayDate = new DateTime(2027, 3, 28), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165011"), Name = "Easter Monday", HolidayDate = new DateTime(2026, 4, 6), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165012"), Name = "Easter Monday", HolidayDate = new DateTime(2027, 3, 29), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165013"), Name = "Anzac Day", HolidayDate = new DateTime(2026, 4, 25), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165014"), Name = "Anzac Day", HolidayDate = new DateTime(2027, 4, 25), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165015"), Name = "Additional Day", HolidayDate = new DateTime(2026, 4, 27), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165016"), Name = "Additional Day", HolidayDate = new DateTime(2027, 4, 26), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165017"), Name = "King's Birthday", HolidayDate = new DateTime(2026, 6, 8), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165018"), Name = "King's Birthday", HolidayDate = new DateTime(2027, 6, 14), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165019"), Name = "Bank Holiday", HolidayDate = new DateTime(2026, 8, 3), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165020"), Name = "Bank Holiday", HolidayDate = new DateTime(2027, 8, 2), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165021"), Name = "Labour Day", HolidayDate = new DateTime(2026, 10, 5), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165022"), Name = "Labour Day", HolidayDate = new DateTime(2027, 10, 4), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165023"), Name = "Christmas Day", HolidayDate = new DateTime(2026, 12, 25), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165024"), Name = "Christmas Day", HolidayDate = new DateTime(2027, 12, 25), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165025"), Name = "Additional Day", HolidayDate = new DateTime(2027, 12, 27), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165026"), Name = "Boxing Day", HolidayDate = new DateTime(2026, 12, 26), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165027"), Name = "Boxing Day", HolidayDate = new DateTime(2027, 12, 26), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165028"), Name = "Additional Day", HolidayDate = new DateTime(2026, 12, 28), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp },
            new PublicHoliday { Id = Guid.Parse("90e87f20-6fd4-4f68-92a8-61fb6f165029"), Name = "Additional Day", HolidayDate = new DateTime(2027, 12, 28), Source = "Seed", IsImported = false, CreatedAt = seedTimestamp }
        );
    }
}
