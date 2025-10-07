# Základní konfigurace
variable "resource_group_name" {
  description = "Název resource group"
  type        = string
  default     = "rg-azure-functions-asb"
}

variable "location" {
  description = "Azure region pro nasazení prostředků"
  type        = string
  default     = "Sweden Central"
}

variable "environment" {
  description = "Prostředí (dev, test, prod)"
  type        = string
  default     = "dev"
  
  validation {
    condition     = contains(["dev", "test", "prod"], var.environment)
    error_message = "Environment must be dev, test, or prod."
  }
}

# Storage Account pro Azure Functions
variable "storage_account_name" {
  description = "Název storage account (musí být globálně jedinečný)"
  type        = string
  default     = "stfuncasb001"
}

variable "storage_account_tier" {
  description = "Performance tier pro storage account"
  type        = string
  default     = "Standard"
}

variable "storage_replication_type" {
  description = "Typ replikace pro storage account"
  type        = string
  default     = "LRS"
}

# App Service Plan
variable "app_service_plan_name" {
  description = "Název App Service Plan"
  type        = string
  default     = "asp-azure-functions-asb"
}

variable "function_app_sku" {
  description = "SKU pro Function App (Y1=Consumption, EP1-3=Premium)"
  type        = string
  default     = "Y1"
}

# Azure Functions
variable "function_app_name" {
  description = "Název Azure Function App"
  type        = string
  default     = "func-azure-functions-asb"
}

variable "dotnet_version" {
  description = "Verze .NET pro Function App"
  type        = string
  default     = "8.0"
  
  validation {
    condition = contains(["6.0", "8.0", "9.0"], var.dotnet_version)
    error_message = ".NET version must be 6.0, 8.0, or 9.0."
  }
}

# Service Bus
variable "servicebus_namespace_name" {
  description = "Název Service Bus namespace"
  type        = string
  default     = "sb-azure-functions-asb"
}

variable "servicebus_sku" {
  description = "SKU pro Service Bus (Basic, Standard, Premium)"
  type        = string
  default     = "Standard"
}

variable "servicebus_queue_name" {
  description = "Název Service Bus queue"
  type        = string
  default     = "asb-queue-test"
}

variable "create_servicebus_topic" {
  description = "Zda vytvořit Service Bus topic"
  type        = bool
  default     = true
}

variable "servicebus_topic_name" {
  description = "Název Service Bus topic"
  type        = string
  default     = "asb-topic-test"
}

# Application Insights
variable "application_insights_name" {
  description = "Název Application Insights"
  type        = string
  default     = "appi-azure-functions-asb"
}

# Tags
variable "tags" {
  description = "Tags pro Azure prostředky"
  type        = map(string)
  default = {
    Environment = "Development"
    Project     = "Azure-Functions-ASB"
    CreatedBy   = "Terraform"
  }
}