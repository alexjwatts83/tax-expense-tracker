# Infra (Terraform Lite)

This folder contains a minimal Terraform + Terragrunt scaffold for a single environment deployment.

## Layout

- `modules/foundation`: shared foundation resources (resource group, shared tags/locals)
- `modules/data`: dormant Azure SQL scaffold; not part of the supported application deployment
- `modules/app`: app hosting resources and Key Vault wiring
- `live/dev`: single environment Terragrunt stacks

## Notes

- Keep module boundaries practical and minimal.
- Avoid heavy abstraction until first deploy is stable.
- Fill in placeholder values in `live/dev/terragrunt.hcl` before first plan/apply.
- The application currently supports SQLite only. Provision persistent single-instance storage for its database file; do not apply `modules/data` unless Azure SQL provider parity is implemented and validated in a separate change.
- Keep `Database__ApplyMigrationsOnStartup=false` in Production and run the explicit EF migration command documented in the root README before routing traffic.
- Configure the hosting readiness probe to `GET /health` and collect API standard output for correlated request logs.
