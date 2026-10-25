# Azure Hardening Rollout Plan

## Plan Status Snapshot

- **Status:** Deferred - Much Later
- **Deferral decision (2026-10-25):** All Azure-related improvements are postponed until explicitly requested. Existing access and input blockers remain recorded; they do not block local launcher or frontend work.
- **Assessed:** 2026-10-25
- **Progress:** 0 of 6 tasks complete
- **Evidence:** All local application hardening, Release validation, migration generation, and Production API smoke checks pass. Azure CLI is not authenticated, no dev stack has been applied, and required Terragrunt inputs remain placeholders.
- **Authority:** This plan is the sole tracker for Azure work transferred from hardening tasks S2-09, S2-10, and the deployed portion of S8-07.

Last Updated: 2026-10-25
Current Task: None - deferred; resume with AZH-01 only on explicit request

## Goal

Deploy the locally hardened application to Azure, configure single-tenant Entra authentication, and prove the deployed security, persistence, monitoring, and workflow contracts without reopening completed local hardening work.

## Scope Transfer

| Source task | Local outcome retained in hardening plan | Azure work owned here |
|---|---|---|
| S2-09 | Validated Entra options, JWT/owner policies, MSAL flow, route guards, and Development-only test scheme | Provision SPA/API registrations, delegated scope, consent, owner assignment, Easy Auth, redirect URIs, and runtime settings |
| S2-10 | Automated anonymous 401, non-owner 403, owner 200, and DataTransfer owner tests | Run the same matrix against the deployed Azure dev environment |
| S8-07 | Release build/tests, migration generation, frontend production build, and live local Production API smoke passed | Run deployed health, migration, logging, frontend, DataTransfer, and role smoke checks and approve Azure sign-off |

## Constraints And Decisions

- SQLite is the sole supported provider. The dormant Azure SQL Terraform module must remain disabled unless provider parity is implemented and validated separately.
- The API requires persistent, single-instance storage for the SQLite database file.
- Production keeps `Database__ApplyMigrationsOnStartup=false`; apply EF migrations explicitly before routing traffic.
- Authentication remains optional in source and disabled by default, but must be enabled before exposing the deployed API.
- Do not commit tenant IDs, client IDs, object IDs, secrets, tokens, or connection strings.

## Tasks

- [!] **AZH-01** Authenticate Azure CLI, confirm subscription/tenant access, replace required owner/cost-center placeholders, and record non-secret environment identifiers outside source control as appropriate. Attempted 2026-10-25; blocked on interactive sign-in and approved tag values.
- [ ] **AZH-02** Align and validate the dev infrastructure plan for Static Web App/API hosting, persistent SQLite storage, Key Vault references, CORS, `/health` probing, and correlated standard-output log collection; keep Azure SQL resources disabled.
- [ ] **AZH-03** Create separate single-tenant SPA and API Entra registrations, expose and consent the delegated API scope, require assignment, assign only the owner, and configure redirect/logout URIs and API Easy Auth.
- [ ] **AZH-04** Provision the dev stack, configure application settings, apply EF migrations explicitly, deploy API/frontend artifacts, and verify `/health` before routing user traffic.
- [ ] **AZH-05** Run the deployed identity matrix: anonymous requests return 401, an authenticated non-owner returns 403 without redirect loops, and the configured owner can use normal and DataTransfer endpoints.
- [ ] **AZH-06** Run deployed workflow and operations smoke checks for login/logout/token renewal, expenses, filters, time tracking, DataTransfer dry-run/import/export, correlation IDs/log lookup, restart persistence, and migration/rollback readiness; update both Azure trackers and approve sign-off.

## Validation Matrix

| Area | Required evidence |
|---|---|
| Infrastructure | `terragrunt plan` reviewed; apply succeeds; required placeholders are gone; Azure SQL module is not applied |
| Startup | Invalid configuration fails actionably; valid Production configuration starts with automatic migrations disabled |
| Persistence | Explicit migration succeeds; seeded/default data is present; SQLite survives API restart on persistent storage |
| Health | `GET /health` returns 200 before traffic is enabled and becomes unhealthy when database access is unavailable |
| Authentication | Anonymous 401, non-owner 403, owner 200; login/logout/deep links/token renewal behave as designed |
| Authorization | DataTransfer is owner-only in navigation and API enforcement; ordinary protected endpoints reject other users |
| Error contract | Deployed 400/401/403/404/409/500 paths retain RFC 9457 shape and correlation IDs where applicable |
| Monitoring | A supplied `X-Correlation-ID` is visible in response headers, ProblemDetails, completion logs, and failure logs |
| Workflows | Expense, filter/reset, time tracking, and DataTransfer dry-run/commit/rollback smoke checks pass |
| Recovery | Backup, migration ordering, rollback decision, and redeploy/restart procedures are documented and exercised |

## Blockers

| Date | Blocker | Resolution | Status |
|---|---|---|---|
| 2026-10-25 | `az account show` still requires `az login` | Authenticate interactively with the target tenant/subscription | Open |
| 2026-10-25 | `infra/live/dev/terragrunt.hcl` still contains owner and cost-center placeholders | Supply approved non-secret values before planning | Open |
| 2026-08-23 | No Azure dev environment or Entra registrations exist | Complete AZH-01 through AZH-04 before deployed validation | Open |

## Validation Log

| Date | Task | Check | Result |
|---|---|---|---|
| 2026-10-25 | AZH-01 | Azure CLI account access and dev Terragrunt inputs | Blocked: CLI requests `az login`; owner and cost-center tags remain `TODO`. No infrastructure changes made. |

## Relationship To Other Plans

- [../archive/FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md](../archive/FULL_STACK_HARDENING_IMPLEMENTATION_PLAN.md) is the completed local application hardening record; do not reopen it for deployment-only work.
- [AZURE_DEPLOYMENT_TRACKER.md](AZURE_DEPLOYMENT_TRACKER.md) remains the broad infrastructure checklist. This plan controls the hardening-specific Azure closure sequence and validation evidence.
- [../NEXT_WORK.md](../NEXT_WORK.md) records this plan as deferred while local improvements take priority.

## Completion Rule

Mark this plan complete only when all six tasks have deployed evidence. Local tests or mocks cannot substitute for Azure provisioning, identity, persistence, and role-matrix validation.

After completion, update `NEXT_WORK.md`, the README, and the broad Azure deployment tracker with the final outcome, then move this plan to `plans/archive/AZURE_HARDENING_ROLLOUT_PLAN.md` and update all links to its archived location.
