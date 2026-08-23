# Next Work

Last Updated: 2026-08-23

Use this file as the starting point when resuming work or asking "what's next?". It is a pointer to the authoritative plan, not a duplicate progress tracker.

## Current Priority

**Task:** Continue Stage 7 with Slice B (`S7-03` + `S7-04`): enforce the `DEC-HARD-005` file-limit contract on API and browser.

**Authoritative plan:** [FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md)

**Current stage:** Stage 7 - DataTransfer Hardening

**Required outcome:**

- Lower the DataTransfer endpoints' API `RequestSizeLimit` from 100 MiB to the `DEC-HARD-005` 12 MiB request limit.
- Reject oversized (>10 MiB) or non-JSON files in the frontend before `file.text()`/`JSON.parse()`.
- Keep API and browser limits in sync so neither can drift.
- Validate with an oversized-payload roundtrip plus focused frontend rejection tests.

**Immediate validation:** Add an oversized API request test and frontend file-rejection tests; keep the full backend suite green.

**Done in this session:** Stage 7 Slice A is complete. Per-entity reference exports query only their owning table (verified by an EF command interceptor), and reference imports roll back the whole payload in one transaction on any entity error. Public holidays are confirmed hard-delete. Backend suite is 203/203.

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
