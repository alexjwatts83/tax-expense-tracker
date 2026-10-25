# Next Work

Last Updated: 2026-10-25

Use this file as the starting point when resuming work or asking "what's next?". It is a pointer to the authoritative plan, not a duplicate progress tracker.

## Current Priority

**Task:** Begin dev launcher task `DL-05`: distinguish unexpected exits and add optional bounded automatic restart.

**Authoritative plan:** [todo/DEV_LAUNCHER_IMPROVEMENTS_PLAN.md](todo/DEV_LAUNCHER_IMPROVEMENTS_PLAN.md)

**Current stage:** Dev Launcher Stage 2 - Readiness and Resilience

**Required outcome:**

- Distinguish user-initiated stops from unexpected exits, reporting Crashed with the exit code and last output lines.
- Add optional automatic restart, disabled by default, with bounded retries and backoff.
- Preserve process ownership safeguards and prevent automatic restart after intentional stops or launcher shutdown.

**Immediate validation:** Run focused exit/restart tests and the launcher build/test suite; verify intentional stops, unexpected exits, and retry limits in the desktop launcher.

**Last completed:** `DL-04` API health readiness passed all 67 launcher tests and the Debug build. Desktop checks confirmed Running with HTTPS 200 and Unhealthy with HTTPS 503 using isolated database configuration. TLS validation remained enabled, smoke services were cleaned up, and the normal database was untouched.

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
