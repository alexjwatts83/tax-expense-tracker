# Next Work

Last Updated: 2026-08-23

Use this file as the starting point when resuming work or asking "what's next?". It is a pointer to the authoritative plan, not a duplicate progress tracker.

## Current Priority

**Task:** Complete Stage 7 Slice C (`S7-05` + `S7-06` + `S7-07`): frontend UX and the stage gate.

**Authoritative plan:** [FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md)

**Current stage:** Stage 7 - DataTransfer Hardening

**Required outcome:**

- Complete authorization states, real cancellation (only where the request is truly abortable), and truthful stage-based progress, reusing existing correlation IDs and error parsing.
- Add a worker-based parse only if measurements against the 10 MiB ceiling justify it.
- Run the full malformed, oversized, unauthorized, dry-run, rollback, and large-roundtrip matrix and pass the stage gate.

**Immediate validation:** Frontend component/service tests for UX states, then the full backend and frontend suites plus the production build for the stage gate.

**Done in this session:** Stage 7 Slices A and B are complete. Per-entity exports query only their owning table; reference imports roll back atomically; a Content-Length guard returns a readable 413 above 12 MiB; the frontend rejects >10 MiB and non-JSON files before reading. Backend suite is 205/205 and frontend is 70/70.

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
