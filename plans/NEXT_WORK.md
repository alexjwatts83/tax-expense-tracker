# Next Work

Last Updated: 2026-08-23

Use this file as the starting point when resuming work or asking "what's next?". It is a pointer to the authoritative plan, not a duplicate progress tracker.

## Current Priority

**Task:** `S1-06` - Replace duplicated component error extraction with the shared frontend utility.

**Authoritative plan:** [FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md)

**Current stage:** Stage 1 - Error Contract and Immediate Defects

**Required outcome:**

- Replace component-local `error?.error` extraction with `parseApiError`.
- Ensure each async error path updates the component message and change detection correctly.
- Preserve each workflow's existing user-facing fallback message.
- Remove obsolete local error helpers and avoid displaying object values as messages.
- Apply the shared parser to DataTransfer export errors so Blob ProblemDetails are decoded.

**Immediate validation:** Add focused component coverage where async parsing changes behavior, then run `npm run test:ci` and `npm run build`. `S1-07` follows with the zero-price expense filter regression fix.

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

- [Azure deployment](todo/AZURE_DEPLOYMENT_TRACKER.md)
- [JSON import/export follow-up](todo/JSON_IMPORT_EXPORT_PLAN.md)

## Completed Records

Completed implementation plans are stored under [archive](archive/). Ongoing launcher usage and maintenance instructions live in the root [README](../README.md).
