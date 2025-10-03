# Example-Test Topic - Test Guide

## Testování Example-Test Topic funkcí

Tento soubor obsahuje návody a příklady pro testování funkcí pracujících s example-test Service Bus topic.

## Přehled funkcí

### 1. SendToExampleTestTopic
- **HTTP Trigger**: POST `/api/SendToExampleTestTopic`
- **Účel**: Odesílá zprávy do example-test topic
- **Authentication**: Function Key

### 2. ProcessExampleTestMessage  
- **Service Bus Trigger**: example-test topic, example-test-subscription
- **Účel**: Zpracovává zprávy z example-test topic
- **Automatické spouštění**: Při příchodu zprávy

## Test Data

### Základní zpráva
```json
{
  "Content": "Test zpráva pro example-test topic",
  "Category": "notification",
  "Priority": "normal",
  "Metadata": {
    "source": "test",
    "version": "1.0"
  }
}
```

### High Priority Notification
```json
{
  "Content": "Kritická notifikace - systém hlásí chybu",
  "Category": "notification", 
  "Priority": "high",
  "Metadata": {
    "severity": "critical",
    "component": "payment-system"
  }
}
```

### Analytics Data
```json
{
  "Content": "User logged in from new device",
  "Category": "analytics",
  "Priority": "normal",
  "Metadata": {
    "userId": "12345",
    "deviceType": "mobile",
    "location": "Prague"
  }
}
```

### Audit Log
```json
{
  "Content": "User permission changed - admin access granted",
  "Category": "audit",
  "Priority": "high",
  "Metadata": {
    "userId": "67890",
    "action": "permission_change",
    "oldRole": "user",
    "newRole": "admin",
    "changedBy": "admin_user"
  }
}
```

### General Message
```json
{
  "Content": "General system message - daily backup completed",
  "Category": "general",
  "Priority": "low",
  "Metadata": {
    "backupSize": "1.2GB",
    "duration": "45min"
  }
}
```

## Testovací příkazy

### 1. Lokální testování (pomocí curl)

```bash
# Test základní zprávy
curl -X POST "http://localhost:7071/api/SendToExampleTestTopic" \
  -H "Content-Type: application/json" \
  -d '{
    "Content": "Test zpráva pro example-test topic",
    "Category": "notification",
    "Priority": "normal"
  }'

# Test high priority zprávy  
curl -X POST "http://localhost:7071/api/SendToExampleTestTopic" \
  -H "Content-Type: application/json" \
  -d '{
    "Content": "Kritická notifikace",
    "Category": "notification",
    "Priority": "high"
  }'
```

### 2. Azure testování (pomocí PowerShell)

```powershell
# Získání Function App URL a klíče
$functionAppName = "func-golden-support-dev-001"
$resourceGroup = "rg-golden-support-dev"

# Získání function key
$functionKey = az functionapp keys list --name $functionAppName --resource-group $resourceGroup --query "functionKeys.default" -o tsv

# Test URL
$functionUrl = "https://$functionAppName.azurewebsites.net/api/SendToExampleTestTopic?code=$functionKey"

# Test základní zprávy
$testData = @{
    Content = "Test zpráva z PowerShell"
    Category = "analytics" 
    Priority = "normal"
    Metadata = @{
        source = "powershell-test"
        timestamp = (Get-Date).ToString("O")
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri $functionUrl -Method POST -Body $testData -ContentType "application/json"
```

### 3. Batch test - více zpráv najednou

```powershell
# PowerShell script pro test více zpráv
$categories = @("notification", "analytics", "audit", "general")
$priorities = @("high", "normal", "low")

for ($i = 1; $i -le 10; $i++) {
    $category = $categories | Get-Random
    $priority = $priorities | Get-Random
    
    $testMessage = @{
        Content = "Batch test zpráva #$i"
        Category = $category
        Priority = $priority
        Metadata = @{
            batchId = "batch-001"
            messageNumber = $i
            timestamp = (Get-Date).ToString("O")
        }
    } | ConvertTo-Json
    
    Write-Host "Sending message $i - Category: $category, Priority: $priority"
    Invoke-RestMethod -Uri $functionUrl -Method POST -Body $testMessage -ContentType "application/json"
    
    Start-Sleep -Milliseconds 500
}
```

## Monitorování a ověření

### 1. Sledování logů v Azure

```bash
# Sledování Function App logů
az webapp log tail --name $functionAppName --resource-group $resourceGroup

# Nebo pomocí Azure CLI s filtrem
az monitor log-analytics query \
  --workspace "$workspaceId" \
  --analytics-query "
    traces 
    | where timestamp > ago(1h)
    | where operation_Name contains 'ExampleTestTopic'
    | project timestamp, message, severityLevel
    | order by timestamp desc
  "
```

### 2. Kontrola Service Bus metrik

```bash
# Počet zpráv v topic
az servicebus topic show \
  --resource-group $resourceGroup \
  --namespace-name $serviceBusNamespace \
  --name "example-test" \
  --query "messageCount"

# Počet zpráv v subscription
az servicebus topic subscription show \
  --resource-group $resourceGroup \
  --namespace-name $serviceBusNamespace \
  --topic-name "example-test" \
  --name "example-test-subscription" \
  --query "messageCount"
```

### 3. Ověření zpracování zpráv

Zkontrolujte logy Function App pro potvrzení zpracování:
- `Processing example-test topic message` - příchod zprávy
- `Processing category: X with priority: Y` - zpracování podle kategorie  
- `Example-test message processed successfully` - úspěšné dokončení

## Troubleshooting

### Časté problémy a řešení

1. **Zpráva se neodesílá**
   - Ověřte connection string pro Service Bus
   - Zkontrolujte Managed Identity permissions
   - Ověřte, že topic existuje

2. **Zpráva se nespracovává**
   - Zkontrolujte subscription existenci
   - Ověřte Service Bus trigger connection
   - Sledujte dead letter queue

3. **Chyby při deserializaci**
   - Ověřte formát JSON zprávy
   - Zkontrolujte správnost Content-Type
   - Validujte povinné fields

### Užitečné monitoring queries

```kusto
// Úspěšně zpracované zprávy za posledních 24 hodin
traces
| where timestamp > ago(1d)
| where message contains "Example-test message processed successfully"
| summarize count() by bin(timestamp, 1h)

// Chyby při zpracování
traces  
| where timestamp > ago(1d)
| where severityLevel >= 3
| where operation_Name contains "ExampleTestTopic"
| project timestamp, message, severityLevel
```

## Performance očekávání

- **High priority zprávy**: zpracování do 150ms
- **Normal priority**: zpracování do 500ms  
- **Low priority**: zpracování do 500ms
- **Throughput**: až 100 zpráv/min (závislí na konfiguraci)