# RBAC - Azure Function přístup k Service Bus
resource "azurerm_role_assignment" "function_servicebus_sender" {
  scope                = azurerm_servicebus_namespace.main.id
  role_definition_name = "Azure Service Bus Data Sender"
  principal_id         = azurerm_linux_function_app.main.identity[0].principal_id
}

# RBAC - Azure Function receiver pro topic (pokud je vytvořen)
resource "azurerm_role_assignment" "function_servicebus_receiver" {
  count                = var.create_servicebus_topic ? 1 : 0
  scope                = azurerm_servicebus_namespace.main.id
  role_definition_name = "Azure Service Bus Data Receiver"
  principal_id         = azurerm_linux_function_app.main.identity[0].principal_id
}