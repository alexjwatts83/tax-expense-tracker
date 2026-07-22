using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Data Source=tax-expense-tracker.db";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite(connectionString, sqlite =>
            sqlite.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

        return new AppDbContext(optionsBuilder.Options);
    }
}
