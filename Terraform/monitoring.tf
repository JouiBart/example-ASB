# Application Insights
resource "azurerm_application_insights" "main" {
  name                = var.application_insights_name
  location           = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  application_type   = "web"

  tags = var.tags
}