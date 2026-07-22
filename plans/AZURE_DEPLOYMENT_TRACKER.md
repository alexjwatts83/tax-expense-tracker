# Azure Deployment Tracker

Last Updated: 2026-07-22
Owner: 
Environment Scope: dev, prod

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
- [ ] Confirm dev/prod subscription strategy

### 2. Application Readiness
- [ ] Confirm env-var based configuration for backend/frontend
- [ ] Confirm production CORS configuration
- [ ] Confirm health checks and startup behavior
- [ ] Confirm migration execution strategy for cloud

### 3. Infrastructure as Code
- [ ] Terraform module structure created
- [ ] Terragrunt live structure created (dev/prod)
- [ ] Remote state backend created/configured
- [ ] Dev infrastructure plan successful
- [ ] Dev infrastructure apply successful

### 4. Security and Secrets
- [ ] Key Vault created
- [ ] Secrets loaded (DB connection, API settings, etc.)
- [ ] App Service configured with Key Vault references
- [ ] Access controls and least privilege reviewed

### 5. Database Cutover
- [ ] Database instance provisioned in Azure
- [ ] Connection string validated from deployed API
- [ ] EF migrations applied in dev
- [ ] Seed/default data strategy validated
- [ ] Backup/rollback process documented

### 6. CI/CD
- [ ] GitHub OIDC federation configured
- [ ] Infra pipeline (plan/apply) configured
- [ ] App pipeline (build/test/deploy) configured
- [ ] Deployment approvals and gates configured

### 7. Validation
- [ ] API smoke tests pass in Azure dev
- [ ] Frontend smoke tests pass in Azure dev
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

## Naming and Tag Standard (Baseline)

### Naming Pattern
Use short, consistent names with environment and region.

`tet-<env>-<service>-<region>-<nn>`

Examples:
- `tet-dev-rg-aue-01`
- `tet-dev-app-api-aue-01`
- `tet-dev-swa-web-aue-01`
- `tet-dev-sql-aue-01`
- `tet-prod-kv-aue-01`

Conventions:
- `env`: `dev`, `prod`
- `region`: `aue` (Australia East)
- `nn`: two-digit sequence (`01`, `02`)
- use lowercase and hyphens only

### Required Resource Tags
Apply these tags to all resources:

- `project = tax-expense-tracker`
- `environment = dev|prod`
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

## Risks and Blockers

| Date | Risk/Blocker | Severity | Mitigation | Owner | Status |
|------|--------------|----------|------------|-------|--------|

## Next Actions (Top 5)
1. Confirm dev/prod subscription strategy.
2. Scaffold Terraform modules and Terragrunt live layout.
3. Create Azure remote state storage and lock strategy.
4. Provision dev core stack in Australia East (API, frontend host, Azure SQL, Key Vault).
5. Validate end-to-end expense flows in Azure dev.

## Notes
- Keep this file updated at least once per work session.
- Update Last Updated date whenever content changes.
- Mark completed tasks with [x].
