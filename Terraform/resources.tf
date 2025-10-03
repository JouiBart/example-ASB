
/*
# Resource Group
resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
}

# Storage Account pro Azure Functions
resource "azurerm_storage_account" "functions_storage" {
  name                     = var.storage_account_name
  resource_group_name      = azurerm_resource_group.main.name
  location                = azurerm_resource_group.main.location
  account_tier             = var.storage_account_tier
  account_replication_type = var.storage_replication_type

  tags = var.tags
}

# App Service Plan
resource "azurerm_service_plan" "functions_plan" {
  name                = var.app_service_plan_name
  resource_group_name = azurerm_resource_group.main.name
  location           = azurerm_resource_group.main.location
  os_type            = "Linux"
  sku_name           = var.function_app_sku

  tags = var.tags
}

# Application Insights
resource "azurerm_application_insights" "main" {
  name                = var.application_insights_name
  location           = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  application_type   = "web"

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
    
    # Service Bus connection
    "ServiceBusConnection__fullyQualifiedNamespace" = "${azurerm_servicebus_namespace.main.name}.servicebus.windows.net"
    
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

# Service Bus Namespace
resource "azurerm_servicebus_namespace" "main" {
  name                = var.servicebus_namespace_name
  location           = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                = var.servicebus_sku

  tags = var.tags
}

# Service Bus Queue
resource "azurerm_servicebus_queue" "main" {
  name         = var.servicebus_queue_name
  namespace_id = azurerm_servicebus_namespace.main.id

  # Konfigurace queue
  max_size_in_megabytes = 1024
  default_message_ttl = "PT1H" # 1 hodina
  
  # Dead letter settings
  dead_lettering_on_message_expiration = true
  max_delivery_count = 3
}

# Service Bus Topic (volitelné)
resource "azurerm_servicebus_topic" "main" {
  count        = var.create_servicebus_topic ? 1 : 0
  name         = var.servicebus_topic_name
  namespace_id = azurerm_servicebus_namespace.main.id

  max_size_in_megabytes = 1024
}

# Service Bus Subscription (pokud je topic vytvořen)
resource "azurerm_servicebus_subscription" "main" {
  count    = var.create_servicebus_topic ? 1 : 0
  name     = "${var.servicebus_topic_name}-subscription"
  topic_id = azurerm_servicebus_topic.main[0].id
  
  max_delivery_count = 3
  dead_lettering_on_message_expiration = true
}

# RBAC - Azure Function přístup k Service Bus
resource "azurerm_role_assignment" "function_servicebus_sender" {
  scope                = azurerm_servicebus_namespace.main.id
  role_definition_name = "Azure Service Bus Data Sender"
  principal_id         = azurerm_linux_function_app.main.identity[0].principal_id
}
*/