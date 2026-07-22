output "sql_server_id" {
  value = azurerm_mssql_server.this.id
}

output "sql_server_fqdn" {
  value = azurerm_mssql_server.this.fully_qualified_domain_name
}

output "sql_database_id" {
  value = azurerm_mssql_database.this.id
}
