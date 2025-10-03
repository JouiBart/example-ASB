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

# Service Bus Topic
resource "azurerm_servicebus_topic" "main" {
  count        = var.create_servicebus_topic ? 1 : 0
  name         = var.servicebus_topic_name
  namespace_id = azurerm_servicebus_namespace.main.id

  max_size_in_megabytes = 1024
  default_message_ttl   = "PT24H" # 24 hodin
  
  # Možnost automatického mazání při neaktivitě
  auto_delete_on_idle = "P30D" # 30 dní
}

# Subscription pro topic
resource "azurerm_servicebus_subscription" "main" {
  count    = var.create_servicebus_topic ? 1 : 0
  name     = "${var.servicebus_topic_name}-subscription"
  topic_id = azurerm_servicebus_topic.main[0].id
  
  max_delivery_count = 5
  dead_lettering_on_message_expiration = true
  default_message_ttl = "PT24H"
  
  # Auto-delete subscription když není aktivní
  auto_delete_on_idle = "P30D"
}