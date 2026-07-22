# Infra (Terraform Lite)

This folder contains a minimal Terraform + Terragrunt scaffold for a single environment deployment.

## Layout

- `modules/foundation`: shared foundation resources (resource group, shared tags/locals)
- `modules/data`: Azure SQL logical server + single database
- `modules/app`: app hosting resources and Key Vault wiring
- `live/dev`: single environment Terragrunt stacks

## Notes

- Keep module boundaries practical and minimal.
- Avoid heavy abstraction until first deploy is stable.
- Fill in placeholder values in `live/dev/terragrunt.hcl` before first plan/apply.
