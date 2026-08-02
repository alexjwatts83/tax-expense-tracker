using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.DataTransfer;

internal static class DataTransferReplaceDeleteUtility
{
    public static int SoftDeleteMissing<T>(
        IReadOnlyCollection<Guid> payloadIds,
        IReadOnlyCollection<T> existingItems,
        Action<T> softDelete,
        bool dryRun)
        where T : class, ISoftDeletableEntity
    {
        var payloadIdSet = payloadIds.ToHashSet();
        var itemsToDelete = existingItems
            .Where(x => !x.IsDeleted && !payloadIdSet.Contains(x.Id))
            .ToList();

        if (!dryRun)
        {
            foreach (var entity in itemsToDelete)
                softDelete(entity);
        }

        return itemsToDelete.Count;
    }
}
