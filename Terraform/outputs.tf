# Output hodnoty pro další použití

# Resource Group
output "resource_group_name" {
  description = "Název vytvořené resource group"
  value       = azurerm_resource_group.main.name
}

output "resource_group_location" {
  description = "Lokace resource group"
  value       = azurerm_resource_group.main.location
}

# Storage Account
output "storage_account_name" {
  description = "Název storage account"
  value       = azurerm_storage_account.functions_storage.name
}

output "storage_account_primary_key" {
  description = "Primary access key pro storage account"
  value       = azurerm_storage_account.functions_storage.primary_access_key
  sensitive   = true
}

# Function App
output "function_app_name" {
  description = "Název Function App"
  value       = azurerm_linux_function_app.main.name
}

output "function_app_hostname" {
  description = "Hostname Function App"
  value       = azurerm_linux_function_app.main.default_hostname
}

output "function_app_url" {
  description = "URL Function App"
  value       = "https://${azurerm_linux_function_app.main.default_hostname}"
}

output "function_app_principal_id" {
  description = "Principal ID systémové Managed Identity"
  value       = azurerm_linux_function_app.main.identity[0].principal_id
}

# Service Bus
output "servicebus_namespace_name" {
  description = "Název Service Bus namespace"
  value       = azurerm_servicebus_namespace.main.name
}

output "servicebus_namespace_hostname" {
  description = "Hostname Service Bus namespace"
  value       = "${azurerm_servicebus_namespace.main.name}.servicebus.windows.net"
}

output "servicebus_queue_name" {
  description = "Název Service Bus queue"
  value       = azurerm_servicebus_queue.main.name
}

output "servicebus_topic_name" {
  description = "Název Service Bus topic (pokud vytvořen)"
  value       = var.create_servicebus_topic ? azurerm_servicebus_topic.main[0].name : null
}

output "servicebus_subscription_name" {
  description = "Název Service Bus subscription (pokud vytvořen)"
  value       = var.create_servicebus_topic ? azurerm_servicebus_subscription.main[0].name : null
}

output "servicebus_connection_string" {
  description = "Service Bus connection string pro Functions"
  value       = "Endpoint=sb://${azurerm_servicebus_namespace.main.name}.servicebus.windows.net/;Authentication=Managed Identity"
  sensitive   = true
}

# Application Insights
output "application_insights_name" {
  description = "Název Application Insights"
  value       = azurerm_application_insights.main.name
}

output "application_insights_instrumentation_key" {
  description = "Instrumentation key pro Application Insights"
  value       = azurerm_application_insights.main.instrumentation_key
  sensitive   = true
}

output "application_insights_connection_string" {
  description = "Connection string pro Application Insights"
  value       = azurerm_application_insights.main.connection_string
  sensitive   = true
}

# Užitečné informace pro deployment
output "deployment_info" {
  description = "Informace o nasazení"
  value = {
    resource_group = azurerm_resource_group.main.name
    location       = azurerm_resource_group.main.location
    function_app   = azurerm_linux_function_app.main.name
    function_url   = "https://${azurerm_linux_function_app.main.default_hostname}"
    servicebus_namespace = azurerm_servicebus_namespace.main.name
    servicebus_queue = azurerm_servicebus_queue.main.name
    environment    = var.environment
  }
}