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
            .ToTable("WorkLocationEntries")
            .HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<LeaveEntry>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Bank>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Tracker>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Tag>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<TaxExpenseTag>().HasQueryFilter(x => !x.TaxExpense!.IsDeleted && !x.Tag!.IsDeleted);

        // Enforce one active entry per calendar date; soft-deleted rows are excluded via the filter.
        modelBuilder.Entity<WorkLocationEntry>()
            .HasIndex(x => x.WorkDate)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = 0");
        modelBuilder.Entity<LeaveEntry>()
            .HasIndex(x => x.LeaveDate)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = 0");

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

        modelBuilder.Entity<TaxExpense>()
            .Metadata.FindNavigation(nameof(TaxExpense.TaxExpenseTags))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<TaxExpenseTag>()
            .HasOne(x => x.Tag)
            .WithMany(x => x.TaxExpenseTags)
            .HasForeignKey(x => x.TagId);

        var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Bank>().HasData(
            Bank.Import(Guid.Parse("f2c328b0-6d89-4b66-8ef4-fcbe9970a1fd"), "ANZ", TimeProvider.System, seedTimestamp, seedTimestamp),
            Bank.Import(Guid.Parse("4c52ddd3-5208-4385-bf85-c1d3e0402ef4"), "CBA", TimeProvider.System, seedTimestamp, seedTimestamp),
            Bank.Import(Guid.Parse("69c4e618-d714-4429-b5a2-3a35eb50b343"), "Westpac", TimeProvider.System, seedTimestamp, seedTimestamp)
        );

        modelBuilder.Entity<Tracker>().HasData(
            Tracker.Import(Guid.Parse("e13e64d9-bf27-4232-9af4-b2db537d5faf"), "H&R Block", "Default tracker", TimeProvider.System, seedTimestamp, seedTimestamp),
            Tracker.Import(Guid.Parse("03f8e309-dff6-4fd8-b1d8-280e7285687e"), "Pluralsight", "Default tracker", TimeProvider.System, seedTimestamp, seedTimestamp),
            Tracker.Import(Guid.Parse("8c8208af-c8d8-40f5-99cc-152f6f2f5fb6"), "Udemy", "Default tracker", TimeProvider.System, seedTimestamp, seedTimestamp),
            Tracker.Import(Guid.Parse("5288117d-0f1f-49ae-9f5f-4f17f8e7d7fb"), "JB Hifi", "Default tracker", TimeProvider.System, seedTimestamp, seedTimestamp),
            Tracker.Import(Guid.Parse("2eec1c4c-3e14-4375-b6ec-c3e696a84734"), "Office Works", "Default tracker", TimeProvider.System, seedTimestamp, seedTimestamp)
        );

        modelBuilder.Entity<PublicHoliday>().HasData(
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165001", "New Year's Day", new DateOnly(2026, 1, 1), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165002", "New Year's Day", new DateOnly(2027, 1, 1), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165003", "Australia Day", new DateOnly(2026, 1, 26), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165004", "Australia Day", new DateOnly(2027, 1, 26), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165005", "Good Friday", new DateOnly(2026, 4, 3), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165006", "Good Friday", new DateOnly(2027, 3, 26), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165007", "Easter Saturday", new DateOnly(2026, 4, 4), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165008", "Easter Saturday", new DateOnly(2027, 3, 27), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165009", "Easter Sunday", new DateOnly(2026, 4, 5), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165010", "Easter Sunday", new DateOnly(2027, 3, 28), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165011", "Easter Monday", new DateOnly(2026, 4, 6), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165012", "Easter Monday", new DateOnly(2027, 3, 29), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165013", "Anzac Day", new DateOnly(2026, 4, 25), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165014", "Anzac Day", new DateOnly(2027, 4, 25), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165015", "Additional Day", new DateOnly(2026, 4, 27), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165016", "Additional Day", new DateOnly(2027, 4, 26), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165017", "King's Birthday", new DateOnly(2026, 6, 8), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165018", "King's Birthday", new DateOnly(2027, 6, 14), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165019", "Bank Holiday", new DateOnly(2026, 8, 3), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165020", "Bank Holiday", new DateOnly(2027, 8, 2), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165021", "Labour Day", new DateOnly(2026, 10, 5), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165022", "Labour Day", new DateOnly(2027, 10, 4), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165023", "Christmas Day", new DateOnly(2026, 12, 25), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165024", "Christmas Day", new DateOnly(2027, 12, 25), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165025", "Additional Day", new DateOnly(2027, 12, 27), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165026", "Boxing Day", new DateOnly(2026, 12, 26), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165027", "Boxing Day", new DateOnly(2027, 12, 26), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165028", "Additional Day", new DateOnly(2026, 12, 28), seedTimestamp),
            CreatePublicHolidaySeed("90e87f20-6fd4-4f68-92a8-61fb6f165029", "Additional Day", new DateOnly(2027, 12, 28), seedTimestamp)
        );
    }

    private static PublicHoliday CreatePublicHolidaySeed(string id, string name, DateOnly holidayDate, DateTime seedTimestamp)
    {
        return PublicHoliday.Import(
            Guid.Parse(id),
            holidayDate,
            name,
            "Seed",
            false,
            TimeProvider.System,
            createdAt: seedTimestamp,
            updatedAt: seedTimestamp);
    }
}
