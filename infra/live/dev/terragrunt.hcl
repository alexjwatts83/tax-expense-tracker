locals {
  env    = "dev"
  region = "australiaeast"
  aue    = "aue"

  project = "tax-expense-tracker"

  tags = {
    project     = local.project
    environment = local.env
    owner       = "TODO"
    managedBy   = "terraform"
    region      = local.region
    costCenter  = "TODO"
    criticality = "low"
  }

  name_prefix = "tet-${local.env}"
}

remote_state {
  backend = "azurerm"

  config = {
    resource_group_name  = "tet-dev-rg-state-aue-01"
    storage_account_name = "tetdevstateaue01"
    container_name       = "tfstate"
    key                  = "${path_relative_to_include()}/terraform.tfstate"
  }
}

generate "provider" {
  path      = "provider.generated.tf"
  if_exists = "overwrite"
  contents  = <<EOF
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
}
EOF
}
