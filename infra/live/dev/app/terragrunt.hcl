include "root" {
  path = find_in_parent_folders()
}

dependency "foundation" {
  config_path = "../foundation"
}

terraform {
  source = "../../../modules/app"
}

inputs = {
  resource_group_name = dependency.foundation.outputs.resource_group_name
  location            = include.root.locals.region
  name_prefix         = include.root.locals.name_prefix
  tags                = include.root.locals.tags
}
