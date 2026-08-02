# Azure Deployment Tracker

Last Updated: 2026-08-02
Owner: 
Environment Scope: dev (single environment)

## Goal
Track Azure deployment implementation progress, key decisions, risks, and rollout readiness.

## Overall Status
- Current Phase: Planning
- Overall Progress: 0%
- Target Go-Live Date: 

## Milestone Checklist

### 1. Platform Decisions
- [x] Select production database provider (MSSQL or PostgreSQL)
- [x] Select Azure region
- [x] Finalize naming convention and resource tags
- [x] Confirm dev/prod subscription strategy

### 2. Application Readiness
- [ ] Confirm env-var based configuration for backend/frontend
- [ ] Confirm production CORS configuration
- [ ] Confirm health checks and startup behavior
- [ ] Confirm migration execution strategy for cloud
- [x] Decide production identity and browser credential flow

### 3. Infrastructure as Code
- [x] Terraform Lite module structure created (minimal modules)
- [x] Terragrunt live structure created (single environment: dev)
- [ ] Remote state backend created/configured
- [ ] Dev infrastructure plan successful
- [ ] Dev infrastructure apply successful

Terraform implementation mode: Terraform Lite (single environment, minimal modules).
Scope for first pass:
- `foundation` module: resource group, naming/tags locals, shared inputs.
- `data` module: Azure SQL logical server + single database.
- `app` module: API host, frontend host, Key Vault wiring.
- Keep module boundaries practical; avoid over-abstraction until first deploy is stable.

### 4. Security and Secrets
- [ ] Key Vault created
- [ ] Secrets loaded (DB connection, API settings, etc.)
- [ ] App Service configured with Key Vault references
- [ ] Create single-tenant SPA and API Entra app registrations
- [ ] Expose and consent the delegated API scope
- [ ] Require assignment and assign only the owner identity
- [ ] Configure API App Service Authentication with the API audience and 401 behavior
- [ ] Configure ASP.NET Core tenant, audience, scope, and allowed object-ID validation
- [ ] Configure Static Web App redirect URIs, API origin, and API CORS origin
- [ ] Validate login, logout, token renewal, wrong-user 403, and DataTransfer access in Azure dev
- [ ] Access controls and least privilege reviewed

### 5. Database Cutover
- [ ] Database instance provisioned in Azure
- [ ] Connection string validated from deployed API
- [ ] EF migrations applied in dev
- [ ] Seed/default data strategy validated
- [ ] Backup/rollback process documented

Execution note:
- For local CLI migration commands in this repository, use Infrastructure as startup project.
- Deployed API still runs `Database.Migrate()` at startup, so rollout should keep migration ordering explicit during release windows.

### 6. CI/CD
- [ ] GitHub OIDC federation configured
- [ ] Infra pipeline (plan/apply) configured
- [ ] App pipeline (build/test/deploy) configured
- [ ] Deployment approvals and gates configured

### 7. Validation
- [ ] API smoke tests pass in Azure dev
- [ ] Frontend manual smoke validation completed in Azure dev (no automated UI tests)
- [ ] Expense flows verified end-to-end
- [ ] Filter/reset behavior verified in Azure
- [ ] Monitoring and alerts configured and tested

### 8. Production Readiness
- [ ] Production plan reviewed
- [ ] Production apply completed
- [ ] Production migration executed
- [ ] Post-deploy validation complete
- [ ] Go-live sign-off complete

## Decision Log
Use this section to record major decisions and rationale.

| Decision ID | Date | Decision | Options Considered | Choice | Rationale | Impact | Owner | Status |
|-------------|------|----------|--------------------|--------|-----------|--------|-------|--------|
| DEC-001 | 2026-07-22 | Production DB engine | MSSQL, PostgreSQL | MSSQL | Best alignment with current .NET/EF workflow and straightforward managed Azure SQL path. | Affects EF provider, infra module design, connection strings, migration process, cost profile | You | Decided |
| DEC-002 | 2026-07-22 | Primary Azure region | East US, Australia East | Australia East (Sydney) | Lower latency and operational proximity for your location near Sydney. | Affects resource placement, latency, failover strategy, and cost. | You | Decided |
| DEC-003 | 2026-07-22 | Naming and tagging standard | Custom per team, Azure baseline standard | Azure baseline naming + mandatory tags | Keeps resource discovery, cost reporting, and policy automation consistent while moving quickly. | Affects all Terraform module inputs, resource naming, cost governance, and operations. | You | Decided |
| DEC-004 | 2026-07-22 | Environment strategy | Separate dev/prod, single environment | Single environment (dev only) | Solo usage; lower operational overhead and faster setup. | Simplifies subscription/RG strategy, Terragrunt structure, and deployment workflow. | You | Decided |
| DEC-005 | 2026-07-23 | UI testing strategy | Automated UI tests, manual UI validation | Manual UI validation only (no automated UI tests) | Solo project; faster delivery and lower maintenance overhead for now. | Validation relies on API automation plus manual frontend smoke/flow checks. | You | Decided |
| DEC-006 | 2026-08-02 | Identity and session architecture | Same-origin Easy Auth cookies, app-owned login, split-origin Entra/MSAL | Single-tenant Entra with separate SPA/API registrations, MSAL code + PKCE, API Easy Auth, and ASP.NET Core JWT/owner validation | Planned Static Web App and API App Service use separate origins. This supplies a hosted sign-in without passwords in the app and restricts access to the sole owner. | Adds app registrations, delegated scope, CORS, redirect URIs, API audience validation, and an owner object-ID setting; no user-management feature is needed. | You | Decided |

## Naming and Tag Standard (Baseline)

### Naming Pattern
Use short, consistent names with environment and region.

`tet-<env>-<service>-<region>-<nn>`

Examples:
- `tet-dev-rg-aue-01`
- `tet-dev-app-api-aue-01`
- `tet-dev-swa-web-aue-01`
- `tet-dev-sql-aue-01`
- `tet-dev-kv-aue-01`

Conventions:
- `env`: `dev` (single environment)
- `region`: `aue` (Australia East)
- `nn`: two-digit sequence (`01`, `02`)
- use lowercase and hyphens only

### Required Resource Tags
Apply these tags to all resources:

- `project = tax-expense-tracker`
- `environment = dev`
- `owner = <name-or-team>`
- `managedBy = terraform`
- `region = australiaeast`
- `costCenter = <value>`
- `criticality = low|medium|high`

Optional useful tags:
- `service = api|frontend|db|keyvault|monitoring`
- `dataClassification = internal|confidential`
- `backup = true|false`

## Change Log
Track meaningful deployment changes and outcomes.

| Date | Change | Environment | Outcome | Notes |
|------|--------|-------------|---------|-------|
| 2026-07-22 | Tracker file created | N/A | Done | Initial baseline |
| 2026-07-22 | Terraform Lite scaffold created (modules + live/dev Terragrunt) | dev | Done | Step 1 complete |
| 2026-07-22 | Remote state strategy wired and bootstrap script added | dev | Done | Step 2 prepared; run bootstrap script to create backend |
| 2026-08-02 | Single-user Entra architecture selected | dev | Planned | Implement immediately after the shared API error contract and before domain refactors |

## Risks and Blockers

| Date | Risk/Blocker | Severity | Mitigation | Owner | Status |
|------|--------------|----------|------------|-------|--------|

## Next Actions (Top 5)
1. Complete the shared API error contract and frontend 401/403 classification.
2. Create the SPA/API Entra registrations and record tenant, client, scope, and owner object IDs outside source control.
3. Add API authentication, frontend MSAL, and Terraform App Service authentication/CORS configuration.
4. Provision or update the Australia East dev stack and validate the sole owner login end to end.
5. Resume date and domain hardening only after the Azure authentication smoke matrix passes.

## Notes
- Keep this file updated at least once per work session.
- Update Last Updated date whenever content changes.
- Mark completed tasks with [x].
