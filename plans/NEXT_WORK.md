# Next Work

Last Updated: 2026-08-23

Use this file as the starting point when resuming work or asking "what's next?". It is a pointer to the authoritative plan, not a duplicate progress tracker.

## Current Priority

**Task:** `S4-03`/`S4-04`/`S4-05` (frontend tag workflow), then `S4-09` (stage gate). The Stage 4 backend is complete.

**Authoritative plan:** [FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md)

**Current stage:** Stage 4 - Expense References and Aggregate Ownership

**Required outcome:**

- S4-03: recover from stale tags without losing unsaved form state (the backend now returns `invalid_reference` when a tag no longer exists).
- S4-04: share manual-tag parsing and resolution across both expense entry points (inline list and expense form).
- S4-05: define and test partial tag-creation failure/retry behavior.
- S4-09: run expense UI, API, EF, and DataTransfer roundtrip tests and pass the stage gate.

**Immediate validation:** Angular suite and production build, then the full backend suite for the gate.

**Done in this session:** S4-01, S4-02, S4-06, S4-07, and S4-08 are complete. Expenses reject missing references and de-duplicate tags; `TaxExpense` owns its tag links through a private backing collection and behavior methods, with an ID-preserving `Import` factory. Backend 112 unit / 54 integration and frontend 48 pass; production build ok.

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
