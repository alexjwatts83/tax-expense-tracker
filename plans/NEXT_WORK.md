# Next Work

Last Updated: 2026-08-23

Use this file as the starting point when resuming work or asking "what's next?". It is a pointer to the authoritative plan, not a duplicate progress tracker.

## Current Priority

**Task:** `S3-07` with migration `S3-08` - Add filtered unique indexes for active work-location and leave dates, then normalization, duplicate preflight, repair, and rollback for existing data. Keep the migration a separate reviewable commit.

**Authoritative plan:** [FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md)

**Current stage:** Stage 3 - Date Integrity and Restore Safety

**Required outcome:**

- Add filtered unique indexes for active (non-deleted) work-location and leave dates.
- Normalize existing non-midnight stored date values to their calendar day.
- Run a duplicate preflight and document deterministic repair before creating indexes.
- Provide a rollback path and review the migration against SQLite and the planned Azure SQL provider.

**Immediate validation:** Run the full backend suite, the EF migration script, the Angular suite, and the production build. `S3-09`/`S3-10` follow with uniqueness-race conflict mapping and the stage gate.

**Done in this session:** S3-04 and S3-05 are complete. Calendar dates are `DateOnly` end to end and serialize as `yyyy-MM-dd`. The `ConvertCalendarDatesToDateOnly` migration is generated (type change and holiday re-seed only). Backend 104 unit / 50 integration and frontend 48 tests pass.

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
