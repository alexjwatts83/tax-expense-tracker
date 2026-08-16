# Next Work

Last Updated: 2026-08-16

Use this file as the starting point when resuming work or asking "what's next?". It is a pointer to the authoritative plan, not a duplicate progress tracker.

## Current Priority

**Task:** `S1-03` - Map backend errors to the agreed ProblemDetails contract and status codes.

**Authoritative plan:** [FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md)

**Current stage:** Stage 1 - Error Contract and Immediate Defects

**Required outcome:**

- Return RFC 9457 `application/problem+json` responses.
- Map validation and invalid-reference failures to `400`.
- Map not-found failures to `404`.
- Map conflict failures to `409`.
- Map unexpected failures to a sanitized, correlated `500`.
- Include stable `type`, `title`, `status`, safe `detail`, request `instance`, `code`, and `correlationId` fields.
- Preserve optional field-error dictionaries for validation failures.

**Immediate validation:** Add or update focused middleware/API tests, then run the backend integration suite. `S1-04` follows and completes explicit 400, 404, 409, and 500 contract coverage.

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
