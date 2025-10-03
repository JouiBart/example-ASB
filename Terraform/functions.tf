# App Service Plan
resource "azurerm_service_plan" "functions_plan" {
  name                = var.app_service_plan_name
  resource_group_name = azurerm_resource_group.main.name
  location           = azurerm_resource_group.main.location
  os_type            = "Linux"
  sku_name           = var.function_app_sku

  tags = var.tags
}

# Azure Function App
resource "azurerm_linux_function_app" "main" {
  name                = var.function_app_name
  resource_group_name = azurerm_resource_group.main.name
  location           = azurerm_resource_group.main.location

  storage_account_name       = azurerm_storage_account.functions_storage.name
  storage_account_access_key = azurerm_storage_account.functions_storage.primary_access_key
  service_plan_id           = azurerm_service_plan.functions_plan.id

  # Managed Identity
  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_stack {
      dotnet_version = var.dotnet_version
      use_dotnet_isolated_runtime = true
    }
    
    # Bezpečnostní nastavení
    ftps_state = "Disabled"
    http2_enabled = true
  }

  # Application settings
  app_settings = {
    "FUNCTIONS_EXTENSION_VERSION" = "~4"
    "FUNCTIONS_WORKER_RUNTIME"   = "dotnet-isolated"
    
    # .NET 9 specific settings
    "DOTNET_FRAMEWORK_VERSION" = "v9.0"
    
    # Service Bus connection (Managed Identity)
    "ServiceBusConnection__fullyQualifiedNamespace" = "${azurerm_servicebus_namespace.main.name}.servicebus.windows.net"
    "ServiceBusConnectionString" = "Endpoint=sb://${azurerm_servicebus_namespace.main.name}.servicebus.windows.net/;Authentication=Managed Identity"
    "ServiceBusQueueName" = var.servicebus_queue_name
    "ServiceBusTopicName" = var.create_servicebus_topic ? var.servicebus_topic_name : ""
    
    # Application Insights
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.main.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.main.connection_string
    
    # Performance settings
    "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING" = azurerm_storage_account.functions_storage.primary_connection_string
    "WEBSITE_CONTENTSHARE" = "${var.function_app_name}-content"
    
    # Security settings
    "WEBSITE_RUN_FROM_PACKAGE" = "1"
    "SCM_DO_BUILD_DURING_DEPLOYMENT" = "false"
  }

  tags = var.tags

  depends_on = [
    azurerm_storage_account.functions_storage,
    azurerm_service_plan.functions_plan,
    azurerm_application_insights.main
  ]
}