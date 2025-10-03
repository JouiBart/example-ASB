# Azure Functions + Service Bus Terraform

Tato modulární Terraform konfigurace vytvoří kompletní infrastrukturu pro Azure Functions s Azure Service Bus integracemi.

## 📁 Modulární struktura

Konfigurace je rozdělena do logických komponent:
- **`resource-group.tf`** - Resource Group
- **`storage.tf`** - Storage Account pro Functions  
- **`functions.tf`** - App Service Plan + Function App (.NET 9)
- **`servicebus.tf`** - Service Bus Namespace, Queue, Topic
- **`monitoring.tf`** - Application Insights
- **`iam.tf`** - RBAC role assignments

Viz **[COMPONENTS.md](COMPONENTS.md)** pro detailní přehled komponent.

## 📋 Vytvořené prostředky

- **Resource Group**: Kontejner pro všechny prostředky
- **Storage Account**: Vyžadován pro Azure Functions runtime
- **App Service Plan**: Hosting plan pro Azure Functions  
- **Azure Function App**: Linux-based s .NET 9 isolated runtime
- **Service Bus Namespace**: Pro messaging
- **Service Bus Queue**: Pro spolehlivé doručování zpráv
- **Service Bus Topic** (volitelné): Pro pub/sub pattern
- **Example-Test Topic**: Ukázkový topic s subscription pro testování
- **Application Insights**: Pro monitoring a diagnostiku
- **Managed Identity**: Bezpečná autentizace
- **RBAC**: Oprávnění pro Function přístup k Service Bus

## 🚀 Použití

### 1. Příprava

```bash
# Přihlášení do Azure
az login

# Nastavení subscription
az account set --subscription "your-subscription-id"
```

### 2. Konfigurace

```bash
# Zkopírování konfigurace
Copy-Item terraform.tfvars.example terraform.tfvars

# Úprava hodnot (zejména názvy - musí být jedinečné)
notepad terraform.tfvars
```

### 3. Deployment

```bash
# Inicializace Terraform
terraform init

# Plánování změn
terraform plan

# Nasazení
terraform apply
```

### 4. Ověření

Po úspěšném nasazení můžete ověřit:

```bash
# Zobrazit výstupní hodnoty
terraform output

# Test Function App
$functionUrl = terraform output -raw function_app_url
Invoke-WebRequest "$functionUrl/api/HttpTrigger1"
```

## ⚙️ Konfigurace

### Důležité proměnné

- `storage_account_name`: Musí být globálně jedinečný
- `function_app_sku`: 
  - `Y1` = Consumption (levné, cold start)
  - `EP1-3` = Premium (rychlejší, vyšší cena)
- `servicebus_sku`: Basic/Standard/Premium

### Environment specifické nasazení

```bash
# Pro různá prostředí použijte různé tfvars soubory
terraform apply -var-file="dev.tfvars"
terraform apply -var-file="prod.tfvars"  
```

## 🔧 Customizace

### Example-Test Topic konfigurace

```hcl
# V terraform.tfvars - povolení example-test topic
create_example_test_topic = true

# Topic se automaticky vytvoří s těmito nastaveními:
# - Název: example-test  
# - Subscription: example-test-subscription
# - TTL: 24 hodin
# - Auto-delete: 30 dní
# - RBAC: Azure Service Bus Data Receiver role
```

### Přidání dalších Service Bus objektů

```hcl
# V terraform.tfvars
create_servicebus_topic = true
servicebus_topic_name = "events-topic"
```

### Změna SKU pro produkci

```hcl
# V terraform.tfvars pro produkci
function_app_sku = "EP1"  # Premium pro lepší výkon
servicebus_sku = "Premium"  # Premium pro větší throughput
```

## 📝 Next Steps

1. **Nasaďte .NET 9 Function kód**:
   ```bash
   # Build a deployment .NET 9 Function App
   cd example-dotnet-function
   func azure functionapp publish <function-app-name>
   ```

2. **Test .NET Functions**:
   ```bash
   # Test HTTP endpoint
   $body = @{ message = "Hello from .NET 9!" } | ConvertTo-Json
   Invoke-RestMethod -Uri "https://<function-app>.azurewebsites.net/api/SendToServiceBus" -Method POST -Body $body -ContentType "application/json"
   ```

3. **Test Example-Test Topic**:
   ```powershell
   # Test example-test topic pomocí PowerShell scriptu
   .\Test-ExampleTestTopic.ps1 -Environment dev -TestType basic
   
   # Test různých typů zpráv
   .\Test-ExampleTestTopic.ps1 -TestType all -MessageCount 5
   
   # Lokální testování
   .\Test-ExampleTestTopic.ps1 -Local -TestType notification
   ```

4. **Nastavte CI/CD pipeline** pro automatické nasazení

3. **Přidejte monitoring alerts** v Application Insights

4. **Implementujte Function kód** pro odesílání zpráv do Service Bus

## 🔐 Bezpečnost

- Managed Identity je automaticky povoleno
- Function má oprávnění "Azure Service Bus Data Sender" 
- Žádné hesla nebo connection stringy v kódu
- Service Bus používá RBAC místo Shared Access Keys

## 🧹 Cleanup

```bash
# Smazání všech prostředků
terraform destroy
```

---

Pro detailnější informace o jednotlivých prostředcích viz dokumentaci Azure Terraform provider.