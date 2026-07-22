include "root" {
  path = find_in_parent_folders()
}

terraform {
  source = "../../../modules/foundation"
}

inputs = {
  resource_group_name = "tet-dev-rg-aue-01"
  location            = include.root.locals.region
  tags                = include.root.locals.tags
}
