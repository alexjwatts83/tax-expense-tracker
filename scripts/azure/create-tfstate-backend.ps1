param(
  [string]$SubscriptionId,

  [string]$Location = "australiaeast",
  [string]$ResourceGroupName = "tet-dev-rg-state-aue-01",
  [string]$StorageAccountName = "tetdevstateaue01",
  [string]$ContainerName = "tfstate"
)

$ErrorActionPreference = "Stop"

function Get-ValueFromParamOrEnv {
  param(
    [string]$ParamValue,
    [string]$EnvName,
    [string]$FallbackValue,
    [bool]$Required
  )

  if (-not [string]::IsNullOrWhiteSpace($ParamValue)) {
    Write-Host "Using parameter value for $EnvName"
    return $ParamValue
  }

  $envValue = [Environment]::GetEnvironmentVariable($EnvName)
  if (-not [string]::IsNullOrWhiteSpace($envValue)) {
    Write-Host "Using environment variable $EnvName"
    return $envValue
  }

  if ($Required) {
    throw "Missing required value. Set -$EnvName equivalent parameter or environment variable $EnvName."
  }

  Write-Host "Environment variable $EnvName not set. Using default '$FallbackValue'."
  return $FallbackValue
}

# Parameter values take precedence; environment variables are fallback.
$SubscriptionId = Get-ValueFromParamOrEnv -ParamValue $SubscriptionId -EnvName "AZURE_SUBSCRIPTION_ID" -FallbackValue "" -Required $true
$Location = Get-ValueFromParamOrEnv -ParamValue $(if ($PSBoundParameters.ContainsKey("Location")) { $Location } else { "" }) -EnvName "TFSTATE_LOCATION" -FallbackValue "australiaeast" -Required $false
$ResourceGroupName = Get-ValueFromParamOrEnv -ParamValue $(if ($PSBoundParameters.ContainsKey("ResourceGroupName")) { $ResourceGroupName } else { "" }) -EnvName "TFSTATE_RESOURCE_GROUP" -FallbackValue "tet-dev-rg-state-aue-01" -Required $false
$StorageAccountName = Get-ValueFromParamOrEnv -ParamValue $(if ($PSBoundParameters.ContainsKey("StorageAccountName")) { $StorageAccountName } else { "" }) -EnvName "TFSTATE_STORAGE_ACCOUNT" -FallbackValue "tetdevstateaue01" -Required $false
$ContainerName = Get-ValueFromParamOrEnv -ParamValue $(if ($PSBoundParameters.ContainsKey("ContainerName")) { $ContainerName } else { "" }) -EnvName "TFSTATE_CONTAINER" -FallbackValue "tfstate" -Required $false

Write-Host "Setting Azure subscription context..."
az account set --subscription $SubscriptionId

Write-Host "Creating resource group if it does not exist..."
az group create `
  --name $ResourceGroupName `
  --location $Location `
  --tags project=tax-expense-tracker environment=dev managedBy=terraform region=australiaeast | Out-Null

Write-Host "Creating storage account if it does not exist..."
az storage account create `
  --name $StorageAccountName `
  --resource-group $ResourceGroupName `
  --location $Location `
  --sku Standard_LRS `
  --kind StorageV2 `
  --min-tls-version TLS1_2 `
  --allow-blob-public-access false | Out-Null

Write-Host "Fetching storage account key..."
$storageKey = az storage account keys list `
  --resource-group $ResourceGroupName `
  --account-name $StorageAccountName `
  --query "[0].value" `
  --output tsv

if ([string]::IsNullOrWhiteSpace($storageKey)) {
  throw "Unable to retrieve storage account key for $StorageAccountName"
}

Write-Host "Creating blob container if it does not exist..."
az storage container create `
  --name $ContainerName `
  --account-name $StorageAccountName `
  --account-key $storageKey `
  --auth-mode key | Out-Null

Write-Host "Remote state backend is ready."
Write-Host "Resource Group: $ResourceGroupName"
Write-Host "Storage Account: $StorageAccountName"
Write-Host "Container: $ContainerName"
