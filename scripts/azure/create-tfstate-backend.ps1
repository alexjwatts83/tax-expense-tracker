param(
  [Parameter(Mandatory = $true)]
  [string]$SubscriptionId,

  [string]$Location = "australiaeast",
  [string]$ResourceGroupName = "tet-dev-rg-state-aue-01",
  [string]$StorageAccountName = "tetdevstateaue01",
  [string]$ContainerName = "tfstate"
)

$ErrorActionPreference = "Stop"

Write-Host "Setting Azure subscription context..."
az account set --subscription $SubscriptionId

Write-Host "Creating resource group if it does not exist..."
az group create \
  --name $ResourceGroupName \
  --location $Location \
  --tags project=tax-expense-tracker environment=dev managedBy=terraform region=australiaeast | Out-Null

Write-Host "Creating storage account if it does not exist..."
az storage account create \
  --name $StorageAccountName \
  --resource-group $ResourceGroupName \
  --location $Location \
  --sku Standard_LRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false | Out-Null

Write-Host "Fetching storage account key..."
$storageKey = az storage account keys list \
  --resource-group $ResourceGroupName \
  --account-name $StorageAccountName \
  --query "[0].value" \
  --output tsv

if ([string]::IsNullOrWhiteSpace($storageKey)) {
  throw "Unable to retrieve storage account key for $StorageAccountName"
}

Write-Host "Creating blob container if it does not exist..."
az storage container create \
  --name $ContainerName \
  --account-name $StorageAccountName \
  --account-key $storageKey \
  --auth-mode key | Out-Null

Write-Host "Remote state backend is ready."
Write-Host "Resource Group: $ResourceGroupName"
Write-Host "Storage Account: $StorageAccountName"
Write-Host "Container: $ContainerName"
