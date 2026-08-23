# Next Work

Last Updated: 2026-08-23

Use this file as the starting point when resuming work or asking "what's next?". It is a pointer to the authoritative plan, not a duplicate progress tracker.

## Current Priority

**Task:** `S4-03`/`S4-04`/`S4-05` (frontend tag workflow) and `S4-06`/`S4-07`/`S4-08` (backend aggregate encapsulation). Two separable slices remain in Stage 4.

**Authoritative plan:** [FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md)

**Current stage:** Stage 4 - Expense References and Aggregate Ownership

**Required outcome:**

- Frontend (S4-03..S4-05): recover from stale tags without losing unsaved form state, share manual-tag parsing/resolution across both expense entry points, and define partial tag-creation failure/retry behavior. Now that the backend rejects missing tag references, stale-tag recovery is required UX.
- Backend (S4-06..S4-08): give `TaxExpense` a private backing collection with explicit tag-link behavior, replace direct collection mutation in Application and DataTransfer, restrict mutation with private setters, and add explicit import/rehydration that preserves IDs. This touches the EF backing-field mapping and the DataTransfer granular tag mutation, so treat it as its own careful slice.

**Immediate validation:** Backend suite, DataTransfer roundtrip, Angular suite, and production build.

**Done in this session:** S4-01 and S4-02 are complete. Expense create/update reject missing source, bank, and tag references and de-duplicate tag IDs; DataTransfer already rejected missing references. Backend 107 unit / 54 integration pass.

**Stage 2 note:** S2-09/S2-10 remain blocked on Azure/Entra provisioning.

## Work Order

1. Continue the first incomplete task in [FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md).
2. Do not begin a dependent stage until the current stage gate passes.
3. Treat [TAX_EXPENSE_TRACKER_PLAN.md](TAX_EXPENSE_TRACKER_PLAN.md) as the high-level product roadmap only.
4. Treat files under [todo](todo/) as intentionally deferred; do not select them unless explicitly requested.
5. Treat files under [archive](archive/) as historical records, not active work queues.

## Update Rules

At the end of each work session:

1. Update task checkboxes, dashboard totals, `Current Stage`, `Next Task`, `Last Updated`, and validation log in the authoritative plan.
2. Update this file only when the current priority, authoritative plan, or deferral decision changes.
3. Keep implementation details and validation evidence in the appropriate plan rather than copying them here.
4. When a plan is complete, ensure durable usage or maintenance information is in the README, then archive the plan.

## Deferred Work

- Stage 2 `S2-09`/`S2-10`: Azure/Entra provisioning and the deployed anonymous/non-owner/owner matrix. Blocked until the target tenant and registration values are available. Keep `Authentication:UseDevelopmentTestScheme` disabled in Azure and Production.
- [Azure deployment](todo/AZURE_DEPLOYMENT_TRACKER.md)
- [JSON import/export follow-up](todo/JSON_IMPORT_EXPORT_PLAN.md)

## Completed Records

Completed implementation plans are stored under [archive](archive/). Ongoing launcher usage and maintenance instructions live in the root [README](../README.md).
