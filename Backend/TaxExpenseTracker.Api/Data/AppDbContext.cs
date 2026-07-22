using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Api.Models;

namespace TaxExpenseTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaxExpense> TaxExpenses => Set<TaxExpense>();
    public DbSet<Tracker> Trackers => Set<Tracker>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TaxExpenseTag> TaxExpenseTags => Set<TaxExpenseTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaxExpense>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Tracker>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Tag>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<TaxExpenseTag>().HasQueryFilter(x => !x.TaxExpense!.IsDeleted && !x.Tag!.IsDeleted);

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

        modelBuilder.Entity<Tracker>().HasData(
            new Tracker { Id = Guid.Parse("e13e64d9-bf27-4232-9af4-b2db537d5faf"), Name = "H&R Block", Description = "Default tracker", IsDeleted = false, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
            new Tracker { Id = Guid.Parse("03f8e309-dff6-4fd8-b1d8-280e7285687e"), Name = "Pluralsight", Description = "Default tracker", IsDeleted = false, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
            new Tracker { Id = Guid.Parse("8c8208af-c8d8-40f5-99cc-152f6f2f5fb6"), Name = "Udemy", Description = "Default tracker", IsDeleted = false, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
            new Tracker { Id = Guid.Parse("5288117d-0f1f-49ae-9f5f-4f17f8e7d7fb"), Name = "JB Hifi", Description = "Default tracker", IsDeleted = false, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
            new Tracker { Id = Guid.Parse("2eec1c4c-3e14-4375-b6ec-c3e696a84734"), Name = "Office Works", Description = "Default tracker", IsDeleted = false, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp }
        );
    }
}