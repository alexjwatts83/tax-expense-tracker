namespace TaxExpenseTracker.Application.DataTransfer;

internal static class DataTransferIssueCodes
{
    public const string WarnInsertOnlySkipped = "WARN_INSERT_ONLY_SKIPPED";
    public const string WarnDuplicateSkipped = "WARN_DUPLICATE_SKIPPED";
    public const string WarnReplaceSoftDeletedMissing = "WARN_REPLACE_SOFT_DELETED_MISSING";
    public const string WarnReplaceDeletedMissing = "WARN_REPLACE_DELETED_MISSING";

    public const string ErrRequiredField = "ERR_REQUIRED_FIELD";
    public const string ErrReferenceNotFound = "ERR_REFERENCE_NOT_FOUND";
    public const string ErrDuplicateConflict = "ERR_DUPLICATE_CONFLICT";
    public const string ErrValidation = "ERR_VALIDATION";
    public const string ErrGeneric = "ERR_GENERIC";
}
