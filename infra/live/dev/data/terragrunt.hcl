include "root" {
  path = find_in_parent_folders()
}

dependency "foundation" {
  config_path = "../foundation"
}

terraform {
  source = "../../../modules/data"
}

inputs = {
  resource_group_name = dependency.foundation.outputs.resource_group_name
  location            = include.root.locals.region
  sql_server_name     = "tet-dev-sql-aue-01"
  sql_database_name   = "tet-dev-sqldb-aue-01"
  sql_database_sku    = "Basic"

  # Set via env var for local runs and CI:
  # export TF_VAR_sql_admin_login=...
  # export TF_VAR_sql_admin_password=...
  sql_admin_login    = get_env("TF_VAR_sql_admin_login", "")
  sql_admin_password = get_env("TF_VAR_sql_admin_password", "")

  tags = include.root.locals.tags
}
