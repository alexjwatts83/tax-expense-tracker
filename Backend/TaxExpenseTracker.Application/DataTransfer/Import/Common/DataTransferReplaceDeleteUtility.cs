using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.DataTransfer;

internal static class DataTransferReplaceDeleteUtility
{
    private const string ReplaceDeleteNotImplementedMessage = "Replace mode delete synchronization is not implemented yet; processing behaves like upsert.";

    public static void AddReplaceDeleteNotImplementedWarning(
        DataTransferImportOptions options,
        ICollection<DataTransferImportIssue> warnings)
    {
        if (options.Mode == DataTransferImportMode.Replace && options.AllowDeletes)
            warnings.Add(new DataTransferImportIssue(
                DataTransferIssueCodes.WarnReplaceDeleteNotImplemented,
                ReplaceDeleteNotImplementedMessage));
    }

    public static async Task<int> SoftDeleteMissingAsync<T>(
        IReadOnlyCollection<Guid> payloadIds,
        Func<CancellationToken, Task<IReadOnlyList<T>>> getAllAsync,
        Func<Guid, CancellationToken, Task<T?>> getByIdAsync,
        Action<T> softDelete,
        bool dryRun,
        CancellationToken cancellationToken)
        where T : class, ISoftDeletableEntity
    {
        var payloadIdSet = payloadIds.ToHashSet();
        var existingItems = await getAllAsync(cancellationToken);
        var idsToDelete = existingItems
            .Where(x => !x.IsDeleted && !payloadIdSet.Contains(x.Id))
            .Select(x => x.Id)
            .ToList();

        if (!dryRun)
        {
            foreach (var id in idsToDelete)
            {
                var entity = await getByIdAsync(id, cancellationToken);
                if (entity is not null && !entity.IsDeleted)
                    softDelete(entity);
            }
        }

        return idsToDelete.Count;
    }

    public static async Task<int> DeleteMissingAsync<T>(
        IReadOnlyCollection<Guid> payloadIds,
        Func<CancellationToken, Task<IReadOnlyList<T>>> getAllAsync,
        Func<IReadOnlyCollection<Guid>, CancellationToken, Task> deleteByIdsAsync,
        bool dryRun,
        CancellationToken cancellationToken)
        where T : class, IEntity
    {
        var payloadIdSet = payloadIds.ToHashSet();
        var existingItems = await getAllAsync(cancellationToken);
        var idsToDelete = existingItems
            .Where(x => !payloadIdSet.Contains(x.Id))
            .Select(x => x.Id)
            .ToList();

        if (!dryRun && idsToDelete.Count > 0)
            await deleteByIdsAsync(idsToDelete, cancellationToken);

        return idsToDelete.Count;
    }
}
