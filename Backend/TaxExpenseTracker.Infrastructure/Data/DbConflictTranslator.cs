using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Infrastructure.Data;

internal static class DbConflictTranslator
{
    // Backstops the application-level date checks: a unique-index race surfaces as the agreed conflict response.
    public static async Task SaveChangesTranslatingConflictsAsync(
        this AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new ApplicationConflictException("A conflicting entry already exists for this date.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
    }
}
