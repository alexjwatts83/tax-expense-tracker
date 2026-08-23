# Next Work

Last Updated: 2026-08-23

Use this file as the starting point when resuming work or asking "what's next?". It is a pointer to the authoritative plan, not a duplicate progress tracker.

## Current Priority

**Task:** `S2-09` / `S2-10` - Provision the Azure Entra resources and run the deployed role matrix. This work is blocked until the target Azure tenant and registration values are available.

**Authoritative plan:** [FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md)

**Current stage:** Stage 2 - Authentication and Authorization

**Required outcome:**

- Create separate single-tenant SPA and API Entra registrations.
- Expose and consent the API scope, configure SPA redirect URIs, and assign the configured owner.
- Configure API Easy Auth, CORS, and matching backend/frontend runtime values.
- Deploy to Azure dev and run anonymous, non-owner, owner, expired-session, and DataTransfer checks.
- Keep `Authentication:UseDevelopmentTestScheme` disabled in Azure and Production.

**Immediate validation:** The local matrix is complete: backend 172/172, frontend 35/35, and the production frontend build pass. Finish with the Azure dev matrix, then close S2-09, S2-10, Stage 2, and release checkpoint R2.

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
