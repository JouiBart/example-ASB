# Azure Service Bus Example Project

A comprehensive example demonstrating Azure Service Bus integration with .NET applications and Infrastructure as Code using Terraform.

## ?? Project Overview

This repository contains a complete example of Azure Service Bus messaging patterns with:

- **Infrastructure as Code** (Terraform) for Azure resources
- **.NET Applications** demonstrating Service Bus messaging
- **Azure Functions** for serverless message processing
- **Complete CI/CD ready setup**

## ??? Project Structure

```
ASB/
??? ASB.Pusher/              # Azure Functions app (Service Bus message sender)
??? ASB.Reader/              # Console app (Service Bus message consumer)  
??? Terraform/               # Infrastructure as Code
?   ??? *.tf                 # Terraform configuration files
?   ??? README.md            # Terraform-specific documentation
?   ??? example-dotnet-function/ # Example Azure Function with Service Bus triggers
??? README.md                # This file
```

## ?? Quick Start

### Prerequisites

- [.NET 8/9 SDK](https://dotnet.microsoft.com/download)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [Terraform](https://www.terraform.io/downloads.html)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local) (optional)
- Azure subscription with appropriate permissions

### 1. Deploy Infrastructure

```bash
# Navigate to Terraform directory
cd Terraform

# Login to Azure
az login

# Configure variables
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values

# Deploy infrastructure
terraform init
terraform plan
terraform apply
```

### 2. Build and Run Applications

```bash
# Build the solution
dotnet build

# Run the message reader (consumer)
cd ASB.Reader
dotnet run

# In another terminal, run the message pusher (producer)
cd ASB.Pusher
func start  # If using Azure Functions locally
# or
dotnet run  # If running as console app
```

## ?? Components

### ASB.Pusher
- **Type**: Azure Functions (.NET 8)
- **Purpose**: Sends messages to Azure Service Bus
- **Features**:
  - HTTP triggers for message sending
  - Timer triggers for scheduled messages
  - Integration with Application Insights
  - Managed Identity authentication

### ASB.Reader  
- **Type**: Console Application (.NET 9)
- **Purpose**: Consumes messages from Azure Service Bus
- **Features**:
  - Service Bus message processing
  - Configurable via appsettings.json
  - Structured logging
  - Graceful shutdown handling

### Terraform Infrastructure
- **Azure Functions** with Linux hosting
- **Azure Service Bus** (Namespace, Queues, Topics)
- **Application Insights** for monitoring
- **Storage Account** for Functions runtime
- **Managed Identity** for secure authentication
- **RBAC** role assignments

See [Terraform/README.md](./Terraform/README.md) for detailed infrastructure documentation.

## ?? Configuration

### Application Settings

Both applications use configuration files and environment variables:

```json
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://...",
    "QueueName": "example-queue",
    "TopicName": "example-topic"
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=..."
  }
}
```

### Environment Variables

- `AZURE_CLIENT_ID` - Managed Identity client ID
- `SERVICEBUS_NAMESPACE` - Service Bus namespace name
- `APPLICATIONINSIGHTS_CONNECTION_STRING` - Application Insights connection

## ?? Messaging Patterns

This project demonstrates several Service Bus messaging patterns:

### 1. Point-to-Point (Queue)
```csharp
// Send message to queue
await sender.SendMessageAsync(new ServiceBusMessage("Hello Queue!"));

// Process queue messages
await processor.StartProcessingAsync();
```

### 2. Publish/Subscribe (Topic)
```csharp
// Send to topic
await sender.SendMessageAsync(new ServiceBusMessage("Hello Topic!"));

// Multiple subscribers can process the same message
```

### 3. Azure Functions Triggers
```csharp
[Function("ProcessServiceBusMessage")]
public void Run([ServiceBusTrigger("myqueue")] ServiceBusReceivedMessage message)
{
    // Process message automatically
}
```

## ?? Security & Authentication

### Managed Identity (Recommended)
```csharp
var client = new ServiceBusClient(
    "yournamespace.servicebus.windows.net", 
    new DefaultAzureCredential());
```

### Connection String (Development)
```csharp
var client = new ServiceBusClient(connectionString);
```

## ?? Testing

### Local Development
1. Use Azure Service Bus connection string for local testing
2. Configure `local.settings.json` for Azure Functions
3. Use Service Bus Explorer for message inspection

### Integration Testing
```bash
# Run integration tests
dotnet test

# Test message flow end-to-end
./Terraform/Test-ExampleTestTopic.ps1
```

## ?? Monitoring & Observability

- **Application Insights** for telemetry and logging
- **Service Bus metrics** in Azure Portal
- **Function execution logs** in Azure Functions monitor
- **Custom dashboards** for message processing insights

## ?? CI/CD Integration

The project is ready for CI/CD with:

- **GitHub Actions** workflows (add `.github/workflows/`)
- **Azure DevOps** pipelines
- **Infrastructure deployment** via Terraform
- **Application deployment** via Azure Functions deployment

Example GitHub Actions workflow:
```yaml
name: Deploy ASB Example
on:
  push:
    branches: [ main ]

jobs:
  infrastructure:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Deploy Infrastructure
        run: |
          cd Terraform
          terraform init
          terraform apply -auto-approve
          
  applications:
    needs: infrastructure
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Deploy Functions
        run: |
          cd ASB.Pusher
          func azure functionapp publish ${{ env.FUNCTION_APP_NAME }}
```

## ?? Learning Resources

- [Azure Service Bus Documentation](https://docs.microsoft.com/azure/service-bus-messaging/)
- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [.NET Azure SDK](https://azure.github.io/azure-sdk-for-net/)
- [Terraform Azure Provider](https://registry.terraform.io/providers/hashicorp/azurerm/latest)

## ?? Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## ?? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ?? Support & Issues

- ?? Check the [Terraform README](./Terraform/README.md) for infrastructure issues
- ?? Report bugs via [GitHub Issues](https://github.com/JouiBart/example-ASB/issues)
- ?? Ask questions in [Discussions](https://github.com/JouiBart/example-ASB/discussions)

---

**Happy messaging with Azure Service Bus!** ??