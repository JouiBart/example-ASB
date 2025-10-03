# Service Bus Managed Identity Configuration

## Problém s Connection String
Azure Service Bus .NET SDK vyžaduje jeden ze způsobů autentizace:
1. **Connection String** s Shared Access Key/Signature
2. **Managed Identity** s namespace URL

❌ **Nesprávně:**
```csharp
// Toto NEFUNGUJE - "Authentication=Managed Identity" není platný connection string
var connectionString = "Endpoint=sb://namespace.servicebus.windows.net/;Authentication=Managed Identity";
var client = new ServiceBusClient(connectionString); // ❌ Exception!
```

✅ **Správně:**
```csharp
// Pro Managed Identity používej pouze namespace
var namespace = "namespace.servicebus.windows.net";
var client = new ServiceBusClient(namespace, new DefaultAzureCredential()); // ✅ Funguje!
```

## Environment Proměnné

### Pro C# ServiceBusClient (Managed Identity):
- `ServiceBusConnection__fullyQualifiedNamespace` = "namespace.servicebus.windows.net"

### Pro Azure Functions Service Bus Trigger:
- `ServiceBusConnection__fullyQualifiedNamespace` = "namespace.servicebus.windows.net"

### Pro aplikační logiku:
- `ServiceBusQueueName` = "test" (název queue)
- `ServiceBusTopicName` = "asb-topic-test" (název topic)

### Pro referenci (nepoužívá se v kódu):
- `ServiceBusConnectionString` = "Endpoint=sb://...;Authentication=Managed Identity"

## Správná implementace

```csharp
public class ServiceBusFunction
{
    private readonly ServiceBusClient _serviceBusClient;

    public ServiceBusFunction()
    {
        // Získej namespace pro Managed Identity
        var namespace = Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace");
        
        // Vytvoř klienta s Managed Identity
        _serviceBusClient = new ServiceBusClient(namespace, new DefaultAzureCredential());
    }

    [Function("SendMessage")]
    public async Task SendMessage()
    {
        var queueName = Environment.GetEnvironmentVariable("ServiceBusQueueName");
        var sender = _serviceBusClient.CreateSender(queueName);
        
        await sender.SendMessageAsync(new ServiceBusMessage("Hello"));
    }

    [Function("ProcessMessage")]
    public async Task ProcessMessage(
        [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message)
    {
        // Zpracuj zprávu
    }
}
```

## RBAC Permissions
Managed Identity potřebuje tyto role:
- `Azure Service Bus Data Sender` - pro odesílání zpráv
- `Azure Service Bus Data Receiver` - pro příjem zpráv z topic

## Lokální development
Pro lokální vývoj se přihlašte do Azure CLI:
```bash
az login
```

DefaultAzureCredential automaticky použije:
1. Azure CLI credentials (lokálně)
2. Managed Identity (v Azure)