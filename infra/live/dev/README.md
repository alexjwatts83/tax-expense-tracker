# Dev Environment (Single Environment)

This folder contains Terragrunt stacks for the single `dev` environment.

Apply order:
1. `foundation`
2. `data`
3. `app`

Before first run:
- Fill in `owner` and `costCenter` in `terragrunt.hcl`
- Set SQL admin credentials via environment variables

Remote state strategy:
- Backend: Azure Storage account via `azurerm` backend.
- Resource group: `tet-dev-rg-state-aue-01`
- Storage account: `tetdevstateaue01`
- Container: `tfstate`
- State key per stack: `${path_relative_to_include()}/terraform.tfstate`
- Locking: Terraform uses Azure Blob lease locking automatically for this backend.

Bootstrap remote state resources:
- Run `scripts/azure/create-tfstate-backend.ps1`
- Then run Terragrunt commands from each stack folder.

Environment variable support for bootstrap script:
- `AZURE_SUBSCRIPTION_ID` (required if `-SubscriptionId` is not passed)
- `TFSTATE_LOCATION` (optional, default: `australiaeast`)
- `TFSTATE_RESOURCE_GROUP` (optional, default: `tet-dev-rg-state-aue-01`)
- `TFSTATE_STORAGE_ACCOUNT` (optional, default: `tetdevstateaue01`)
- `TFSTATE_CONTAINER` (optional, default: `tfstate`)

Example (PowerShell):
```powershell
$env:AZURE_SUBSCRIPTION_ID = "<subscription-id>"
$env:TFSTATE_LOCATION = "australiaeast"
$env:TFSTATE_RESOURCE_GROUP = "tet-dev-rg-state-aue-01"
$env:TFSTATE_STORAGE_ACCOUNT = "tetdevstateaue01"
$env:TFSTATE_CONTAINER = "tfstate"

powershell -ExecutionPolicy Bypass -File "scripts/azure/create-tfstate-backend.ps1"
```
