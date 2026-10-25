# Next Work

Last Updated: 2026-08-23

Use this file as the starting point when resuming work or asking "what's next?". It is a pointer to the authoritative plan, not a duplicate progress tracker.

## Current Priority

**Task:** Begin Azure hardening rollout task `AZH-01`: establish Azure access and final inputs.

**Authoritative plan:** [todo/AZURE_HARDENING_ROLLOUT_PLAN.md](todo/AZURE_HARDENING_ROLLOUT_PLAN.md)

**Current stage:** Azure Hardening Rollout

**Required outcome:**

- Authenticate Azure CLI and confirm target subscription/tenant access.
- Finalize required non-secret Terragrunt inputs without committing credentials or identifiers that should remain private.
- Proceed through infrastructure, Entra, deployment, role-matrix, and operational smoke tasks in the Azure rollout plan.

**Immediate validation:** Run `az login`, confirm `az account show`, and resolve the owner/cost-center placeholders before planning infrastructure.

**Done in this session:** The full-stack hardening plan is complete at 74/74 local tasks. Azure provisioning and deployed validation were separated into the dedicated Azure hardening rollout plan so tenant-dependent work no longer blocks the completed application hardening record.

**Hardening note:** [archive/FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](archive/FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md) is complete. Do not reopen it for deployment-only work.

## Work Order

1. Continue the first incomplete task in [todo/AZURE_HARDENING_ROLLOUT_PLAN.md](todo/AZURE_HARDENING_ROLLOUT_PLAN.md).
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

- [Azure deployment tracker](todo/AZURE_DEPLOYMENT_TRACKER.md)
- Keep `Authentication:UseDevelopmentTestScheme` disabled in Azure and Production.

## Deferred Work

- [JSON import/export follow-up](todo/JSON_IMPORT_EXPORT_PLAN.md)
- [Dev launcher improvements](todo/DEV_LAUNCHER_IMPROVEMENTS_PLAN.md)
- [Frontend quality improvements](todo/FRONTEND_QUALITY_IMPROVEMENTS_PLAN.md)

## Completed Records

Completed implementation plans are stored under [archive](archive/). Ongoing launcher usage and maintenance instructions live in the root [README](../README.md).
