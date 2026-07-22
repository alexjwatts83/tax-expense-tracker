variable "resource_group_name" {
  description = "Resource group name"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "sql_server_name" {
  description = "Azure SQL logical server name"
  type        = string
}

variable "sql_database_name" {
  description = "Azure SQL database name"
  type        = string
}

variable "sql_database_sku" {
  description = "Azure SQL database SKU"
  type        = string
  default     = "Basic"
}

variable "sql_admin_login" {
  description = "Azure SQL admin username"
  type        = string
}

variable "sql_admin_password" {
  description = "Azure SQL admin password"
  type        = string
  sensitive   = true
}

variable "tags" {
  description = "Common tags applied to resources"
  type        = map(string)
  default     = {}
}
