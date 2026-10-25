# Next Work

Last Updated: 2026-10-25

Use this file as the starting point when resuming work or asking "what's next?". It is a pointer to the authoritative plan, not a duplicate progress tracker.

## Current Priority

**Task:** Begin dev launcher task `DL-06`: select the API launch profile and build configuration, deriving ports from launch settings.

**Authoritative plan:** [todo/DEV_LAUNCHER_IMPROVEMENTS_PLAN.md](todo/DEV_LAUNCHER_IMPROVEMENTS_PLAN.md)

**Current stage:** Dev Launcher Stage 2 - Readiness and Resilience

**Required outcome:**

- Allow selecting the API launch profile (`https` or `http`) and build configuration (`Debug` or `Release`).
- Read selected-profile ports from the API's `Properties/launchSettings.json` rather than duplicating them.
- Keep service commands, local URLs, health readiness, and ownership checks consistent with the selected profile.

**Immediate validation:** Run focused launch-profile tests and the launcher build/test suite; verify selected commands, ports, and readiness in the desktop launcher.

**Last completed:** `DL-05` unexpected-exit reporting and default-off bounded restart passed all 77 launcher tests and the Debug build. Desktop checks confirmed signed crash exit codes/output, intentional Stopped state, and opt-in retry exhaustion. Original settings/environment were restored and smoke services cleaned up without touching the normal database.

**Priority decision (2026-10-25):** All Azure-related work is deferred to much later, until explicitly requested. Do not attempt Azure sign-in, provisioning, or deployed validation as part of local improvements. Complete dev launcher improvements next, followed by frontend quality improvements.

**Hardening note:** [archive/FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](archive/FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md) is complete. Do not reopen it for deployment-only work.

## Work Order

1. Continue the first incomplete task in [todo/DEV_LAUNCHER_IMPROVEMENTS_PLAN.md](todo/DEV_LAUNCHER_IMPROVEMENTS_PLAN.md), then select [todo/FRONTEND_QUALITY_IMPROVEMENTS_PLAN.md](todo/FRONTEND_QUALITY_IMPROVEMENTS_PLAN.md) as the next authoritative plan.
2. Do not begin a dependent stage until the current stage gate passes.
3. Treat [TAX_EXPENSE_TRACKER_PLAN.md](TAX_EXPENSE_TRACKER_PLAN.md) as the high-level product roadmap only.
4. Treat files under [todo](todo/) as intentionally deferred unless this file explicitly names one as the authoritative plan.
5. Treat files under [archive](archive/) as historical records, not active work queues.

## Update Rules

At the end of each work session:

1. Update task checkboxes, progress, `Current Task`, `Last Updated`, blockers, and validation evidence in the authoritative plan.
2. Update this file only when the current priority, authoritative plan, or deferral decision changes.
3. Keep implementation details and validation evidence in the appropriate plan rather than copying them here.
4. When a plan is complete, ensure durable usage or maintenance information is in the README, then archive the plan.

## Supporting Work

- [Frontend quality improvements](todo/FRONTEND_QUALITY_IMPROVEMENTS_PLAN.md) follow the launcher plan; neither requires Azure access.

## Deferred Work

- [JSON import/export follow-up](todo/JSON_IMPORT_EXPORT_PLAN.md)
- [Azure hardening rollout](todo/AZURE_HARDENING_ROLLOUT_PLAN.md): much later; resume only on explicit request.
- [Azure deployment tracker](todo/AZURE_DEPLOYMENT_TRACKER.md): much later; resume only on explicit request.

## Completed Records

Completed implementation plans are stored under [archive](archive/). Ongoing launcher usage and maintenance instructions live in the root [README](../README.md).
