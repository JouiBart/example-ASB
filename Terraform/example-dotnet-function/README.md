# Azure Functions .NET 9 Example

Tento projekt obsahuje ukázkové Azure Functions v .NET 9 s izolovaným runtime pro integraci se Service Bus.

## 🚀 Funkcionality

- **HTTP Trigger**: `SendToServiceBus` - přijme HTTP požadavek a odešle zprávu do Service Bus
- **Service Bus Trigger**: `ProcessServiceBusMessage` - automaticky zpracuje zprávy ze Service Bus queue

## 📋 Požadavky

- **.NET 9.0 SDK**
- **Azure Functions Core Tools v4**
- **Azure CLI** (pro deployment)

## 🛠️ Lokální development

### 1. Instalace závislostí

```bash
# Obnovení NuGet packages
dotnet restore
```

### 2. Konfigurace

```bash
# Upravte local.settings.json s vašimi hodnotami
# - ServiceBusConnection__fullyQualifiedNamespace
# - APPINSIGHTS_INSTRUMENTATIONKEY
# - MESSAGE_QUEUE_NAME
```

### 3. Spuštění lokálně

```bash
# Spuštění Functions runtime
func start

# Functions budou dostupné na:
# http://localhost:7071/api/SendToServiceBus
```

### 4. Test funkcí

```powershell
# Test HTTP funkce
$body = @{
    message = "Hello from .NET 9!"
    priority = "high"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/SendToServiceBus" -Method POST -Body $body -ContentType "application/json"
```

## 🏗️ Build a deployment

### Lokální build

```bash
dotnet build
dotnet publish --configuration Release
```

### Deployment do Azure

```bash
# Z root Terraform adresáře po nasazení infrastruktury
cd example-dotnet-function

# Deployment Function App
func azure functionapp publish <function-app-name>
```

## 🔧 Konfigurace

### Application Settings (automaticky nastavené Terraformem)

- `FUNCTIONS_WORKER_RUNTIME`: `dotnet-isolated`
- `FUNCTIONS_EXTENSION_VERSION`: `~4`
- `ServiceBusConnection__fullyQualifiedNamespace`: Service Bus namespace
- `APPINSIGHTS_INSTRUMENTATIONKEY`: Application Insights key

### Managed Identity

Functions automaticky používají System-Assigned Managed Identity pro:
- Přístup k Service Bus (Azure Service Bus Data Sender role)
- Application Insights telemetry

## 📝 Příklady použití

### Odeslání zprávy přes HTTP

```bash
POST /api/SendToServiceBus
Content-Type: application/json

{
  "message": "Vaše zpráva",
  "messageId": "optional-custom-id",
  "priority": "high"
}
```

### Odpověď

```json
{
  "success": true,
  "messageId": "12345-67890",
  "queueName": "messages-queue",
  "timestamp": "2025-10-03T10:30:00.000Z"
}
```

### Automatické zpracování ze Service Bus

Funkce `ProcessServiceBusMessage` se spustí automaticky při příchodu zprávy do queue a:
- Loguje informace o zprávě
- Zpracuje business logiku na základě priority
- Automaticky potvrdí nebo odmítne zprávu

## 🔍 Monitoring

### Application Insights

Všechny funkce automaticky logují do Application Insights:
- Request/Response metriky
- Custom log messages
- Exception tracking
- Performance counters

### Queries v Application Insights

```kusto
// Úspěšné požadavky na SendToServiceBus
requests
| where name == "SendToServiceBus"
| where success == true
| summarize count() by bin(timestamp, 5m)

// Service Bus message processing
traces
| where message contains "Processing Service Bus message"
| project timestamp, message, customDimensions
```

## 🚨 Error Handling

Functions implementují robustní error handling:
- **HTTP Functions**: Vrací appropriate HTTP status kódy
- **Service Bus Functions**: Re-throw exceptions pro retry mechanismus
- **Logging**: Strukturované logování pro lepší troubleshooting

## 🔐 Bezpečnost

- **Managed Identity**: Žádné connection strings v kódu
- **Authorization Level**: Function level pro HTTP triggers
- **HTTPS Only**: Veškerá komunikace přes HTTPS
- **Input Validation**: Validace všech příchozích dat

---

Pro deployment této Function App použijte Terraform konfiguraci v root adresáři projektu.