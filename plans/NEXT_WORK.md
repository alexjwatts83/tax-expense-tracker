# Next Work

Last Updated: 2026-08-23

Use this file as the starting point when resuming work or asking "what's next?". It is a pointer to the authoritative plan, not a duplicate progress tracker.

## Current Priority

**Task:** Stage 6 Slice B (`S6-02`) - Centralize genuinely shared day-entry hours rules without hiding work-location and leave terminology.

**Authoritative plan:** [FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md)

**Current stage:** Stage 6 - Remaining Domain Encapsulation

**Required outcome:**

- Centralize the identical full-day, half-day, and specific-hours validation/calculation rules used by both time-entry aggregates (Slice B: S6-02).
- Keep `LeaveEntry` and `WorkLocationEntry` factories, update methods, exceptions, and feature terminology explicit.
- Encapsulate reference entities and implement audit timestamps once `DEC-HARD-006` is decided (Slice C: S6-03 + reference-entity part of S6-04 + S6-05).
- Note: Slice C is blocked until `DEC-HARD-006` (reference entity audit timestamps) is resolved in the decision log.

**Immediate validation:** Focused domain tests for full-day, half-day, valid/invalid specific hours, and invalid entry types in both aggregates, followed by service and full backend suites.

**Done in this session:** Stage 6 Slice A is complete. `LeaveEntry` and `WorkLocationEntry` expose encapsulated state, retain explicit validated create/update behavior, and use narrow import factories for identity restoration. EF SQLite materialization and DataTransfer roundtrip pass; the backend suite is 190/190.

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
