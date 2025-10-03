# Test script pro example-test Service Bus topic
# Použití: .\Test-ExampleTestTopic.ps1 -Environment dev -TestType basic

param(
    [Parameter(Mandatory = $false)]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory = $false)]
    [string]$TestType = "basic",
    
    [Parameter(Mandatory = $false)]
    [string]$FunctionAppName = "",
    
    [Parameter(Mandatory = $false)]
    [string]$ResourceGroup = "",
    
    [Parameter(Mandatory = $false)]
    [int]$MessageCount = 1,
    
    [Parameter(Mandatory = $false)]
    [switch]$Local
)

# Konfigurace pro prostředí
$config = @{
    dev = @{
        functionApp = "func-golden-support-dev-001"
        resourceGroup = "rg-golden-support-dev"
        serviceBusNamespace = "sb-golden-support-dev"
        location = "West Europe"
    }
    prod = @{
        functionApp = "func-golden-support-prod-001" 
        resourceGroup = "rg-golden-support-prod"
        serviceBusNamespace = "sb-golden-support-prod"
        location = "West Europe"
    }
}

# Použití parametrů nebo výchozích hodnot
$functionAppName = if ($FunctionAppName) { $FunctionAppName } else { $config[$Environment].functionApp }
$resourceGroupName = if ($ResourceGroup) { $ResourceGroup } else { $config[$Environment].resourceGroup }
$serviceBusNamespaceName = $config[$Environment].serviceBusNamespace

Write-Host "=== Test Example-Test Topic ===" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "Function App: $functionAppName" -ForegroundColor Cyan
Write-Host "Resource Group: $resourceGroupName" -ForegroundColor Cyan
Write-Host "Test Type: $TestType" -ForegroundColor Cyan
Write-Host "Message Count: $MessageCount" -ForegroundColor Cyan
Write-Host ""

# Test data pro různé typy zpráv
$testMessages = @{
    basic = @{
        Content = "Basic test message for example-test topic"
        Category = "notification"
        Priority = "normal"
        Metadata = @{
            source = "powershell-test"
            testType = "basic"
            timestamp = (Get-Date).ToString("O")
        }
    }
    
    notification = @{
        Content = "High priority notification - system alert detected"
        Category = "notification"
        Priority = "high"
        Metadata = @{
            alertType = "system"
            severity = "critical"
            component = "payment-gateway"
            source = "monitoring-system"
        }
    }
    
    analytics = @{
        Content = "User behavior analytics data - login from new device"
        Category = "analytics"
        Priority = "normal"
        Metadata = @{
            userId = "user-12345"
            deviceType = "mobile"
            location = "Prague, CZ"
            sessionId = "session-67890"
            source = "analytics-engine"
        }
    }
    
    audit = @{
        Content = "Security audit log - user permission elevation"
        Category = "audit" 
        Priority = "high"
        Metadata = @{
            userId = "admin-user"
            action = "permission_change"
            oldRole = "user"
            newRole = "administrator"
            requestId = "req-audit-001"
            source = "security-system"
        }
    }
    
    general = @{
        Content = "General system message - daily maintenance completed"
        Category = "general"
        Priority = "low"
        Metadata = @{
            maintenanceType = "daily-backup"
            duration = "45 minutes"
            dataSize = "2.1GB"
            source = "maintenance-system"
        }
    }
}

# Funkce pro získání Function App URL a klíče
function Get-FunctionAppDetails {
    param($functionApp, $resourceGroup)
    
    try {
        Write-Host "Getting Function App details..." -ForegroundColor Yellow
        
        # Získání function key
        $functionKey = az functionapp keys list --name $functionApp --resource-group $resourceGroup --query "functionKeys.default" -o tsv
        
        if (-not $functionKey) {
            throw "Failed to get function key"
        }
        
        # Sestavení URL
        $functionUrl = "https://$functionApp.azurewebsites.net/api/SendToExampleTestTopic?code=$functionKey"
        
        return @{
            Url = $functionUrl
            Key = $functionKey
        }
    }
    catch {
        Write-Error "Failed to get Function App details: $($_.Exception.Message)"
        return $null
    }
}

# Funkce pro odeslání test zprávy
function Send-TestMessage {
    param($functionUrl, $message, $messageNumber = 1)
    
    try {
        $jsonMessage = $message | ConvertTo-Json -Depth 10
        
        Write-Host "Sending message $messageNumber..." -ForegroundColor Yellow
        Write-Host "Category: $($message.Category), Priority: $($message.Priority)" -ForegroundColor Gray
        
        $response = Invoke-RestMethod -Uri $functionUrl -Method POST -Body $jsonMessage -ContentType "application/json"
        
        Write-Host "✓ Message sent successfully" -ForegroundColor Green
        Write-Host "  MessageId: $($response.MessageId)" -ForegroundColor Gray
        Write-Host "  TopicName: $($response.TopicName)" -ForegroundColor Gray
        Write-Host ""
        
        return $response
    }
    catch {
        Write-Error "Failed to send message $messageNumber`: $($_.Exception.Message)"
        return $null
    }
}

# Funkce pro kontrolu Service Bus metrik
function Check-ServiceBusMetrics {
    param($namespace, $resourceGroup)
    
    try {
        Write-Host "Checking Service Bus metrics..." -ForegroundColor Yellow
        
        # Počet zpráv v topic
        $topicInfo = az servicebus topic show --resource-group $resourceGroup --namespace-name $namespace --name "example-test" | ConvertFrom-Json
        
        # Počet zpráv v subscription
        $subscriptionInfo = az servicebus topic subscription show --resource-group $resourceGroup --namespace-name $namespace --topic-name "example-test" --name "example-test-subscription" | ConvertFrom-Json
        
        Write-Host "Topic 'example-test':" -ForegroundColor Cyan
        Write-Host "  Status: $($topicInfo.status)" -ForegroundColor Gray
        Write-Host "  Size: $($topicInfo.sizeInBytes) bytes" -ForegroundColor Gray
        
        Write-Host "Subscription 'example-test-subscription':" -ForegroundColor Cyan  
        Write-Host "  Status: $($subscriptionInfo.status)" -ForegroundColor Gray
        Write-Host "  Message Count: $($subscriptionInfo.messageCount)" -ForegroundColor Gray
        Write-Host "  Active Message Count: $($subscriptionInfo.countDetails.activeMessageCount)" -ForegroundColor Gray
        Write-Host ""
    }
    catch {
        Write-Warning "Could not retrieve Service Bus metrics: $($_.Exception.Message)"
    }
}

# Funkce pro sledování Function App logů
function Monitor-FunctionLogs {
    param($functionApp, $resourceGroup, $durationSeconds = 30)
    
    Write-Host "Monitoring Function App logs for $durationSeconds seconds..." -ForegroundColor Yellow
    Write-Host "Press Ctrl+C to stop monitoring" -ForegroundColor Gray
    Write-Host ""
    
    try {
        # Spuštění log streaming v pozadí
        $logJob = Start-Job -ScriptBlock {
            param($app, $rg, $duration)
            az webapp log tail --name $app --resource-group $rg
        } -ArgumentList $functionApp, $resourceGroup, $durationSeconds
        
        # Čekání na dokončení
        Wait-Job $logJob -Timeout $durationSeconds | Out-Null
        
        # Získání výstupu
        $logs = Receive-Job $logJob
        Remove-Job $logJob
        
        if ($logs) {
            Write-Host "Recent logs:" -ForegroundColor Cyan
            $logs | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        }
    }
    catch {
        Write-Warning "Could not monitor logs: $($_.Exception.Message)"
    }
}

# Hlavní test logika
try {
    # Kontrola Azure CLI přihlášení
    $accountInfo = az account show | ConvertFrom-Json
    if (-not $accountInfo) {
        throw "Not logged in to Azure CLI. Run 'az login' first."
    }
    
    Write-Host "Logged in as: $($accountInfo.user.name)" -ForegroundColor Green
    Write-Host ""
    
    if ($Local) {
        # Lokální testování
        $functionUrl = "http://localhost:7071/api/SendToExampleTestTopic"
        Write-Host "Testing locally at: $functionUrl" -ForegroundColor Cyan
    } else {
        # Azure testování
        $functionDetails = Get-FunctionAppDetails -functionApp $functionAppName -resourceGroup $resourceGroupName
        if (-not $functionDetails) {
            throw "Failed to get Function App details"
        }
        $functionUrl = $functionDetails.Url
    }
    
    # Kontrola Service Bus před testem (pouze pro Azure)
    if (-not $Local) {
        Check-ServiceBusMetrics -namespace $serviceBusNamespaceName -resourceGroup $resourceGroupName
    }
    
    # Výběr test zpráv podle typu
    $messagesToSend = @()
    
    switch ($TestType.ToLower()) {
        "basic" { $messagesToSend = @($testMessages.basic) }
        "notification" { $messagesToSend = @($testMessages.notification) }
        "analytics" { $messagesToSend = @($testMessages.analytics) }
        "audit" { $messagesToSend = @($testMessages.audit) }
        "general" { $messagesToSend = @($testMessages.general) }
        "all" { 
            $messagesToSend = @(
                $testMessages.basic,
                $testMessages.notification, 
                $testMessages.analytics,
                $testMessages.audit,
                $testMessages.general
            )
        }
        "batch" {
            # Vytvoření batch zpráv
            $categories = @("notification", "analytics", "audit", "general")
            $priorities = @("high", "normal", "low")
            
            for ($i = 1; $i -le $MessageCount; $i++) {
                $category = $categories | Get-Random
                $priority = $priorities | Get-Random
                
                $batchMessage = @{
                    Content = "Batch test message #$i - randomly generated"
                    Category = $category
                    Priority = $priority
                    Metadata = @{
                        batchId = "batch-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
                        messageNumber = $i
                        totalMessages = $MessageCount
                        timestamp = (Get-Date).ToString("O")
                        source = "powershell-batch-test"
                    }
                }
                $messagesToSend += $batchMessage
            }
        }
        default { 
            Write-Warning "Unknown test type: $TestType. Using 'basic' instead."
            $messagesToSend = @($testMessages.basic)
        }
    }
    
    # Odeslání zpráv
    Write-Host "Sending $($messagesToSend.Count) message(s)..." -ForegroundColor Green
    $results = @()
    
    for ($i = 0; $i -lt $messagesToSend.Count; $i++) {
        $result = Send-TestMessage -functionUrl $functionUrl -message $messagesToSend[$i] -messageNumber ($i + 1)
        if ($result) {
            $results += $result
        }
        
        # Pauza mezi zprávami pro batch test
        if ($TestType -eq "batch" -and $i -lt $messagesToSend.Count - 1) {
            Start-Sleep -Milliseconds 500
        }
    }
    
    # Shrnutí výsledků
    Write-Host "=== Test Results ===" -ForegroundColor Green
    Write-Host "Messages sent: $($results.Count)" -ForegroundColor Cyan
    Write-Host "Success rate: $(($results.Count / $messagesToSend.Count * 100).ToString('F1'))%" -ForegroundColor Cyan
    
    if ($results.Count -gt 0) {
        Write-Host "Message IDs:" -ForegroundColor Gray
        $results | ForEach-Object { Write-Host "  - $($_.MessageId)" -ForegroundColor Gray }
    }
    
    # Kontrola Service Bus po testu (pouze pro Azure)
    if (-not $Local) {
        Write-Host ""
        Start-Sleep -Seconds 2
        Check-ServiceBusMetrics -namespace $serviceBusNamespaceName -resourceGroup $resourceGroupName
        
        # Volitelné sledování logů
        $monitorLogs = Read-Host "Monitor Function App logs? (y/n)"
        if ($monitorLogs -eq "y" -or $monitorLogs -eq "yes") {
            Monitor-FunctionLogs -functionApp $functionAppName -resourceGroup $resourceGroupName -durationSeconds 30
        }
    }
    
    Write-Host ""
    Write-Host "✓ Test completed successfully!" -ForegroundColor Green
}
catch {
    Write-Error "Test failed: $($_.Exception.Message)"
    exit 1
}