#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Modulární deployment script pro Azure Functions + Service Bus

.DESCRIPTION
    Deployment script s podporou pro modulární Terraform strukturu

.PARAMETER Action
    plan, apply, destroy, validate-modules

.PARAMETER Module
    Specifický modul k nasazení (functions, servicebus, monitoring, iam, all)

.EXAMPLE
    .\deploy-modular.ps1 -Action plan
    .\deploy-modular.ps1 -Action apply -Module functions
    .\deploy-modular.ps1 -Action validate-modules
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("plan", "apply", "destroy", "validate-modules")]
    [string]$Action,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("functions", "servicebus", "monitoring", "iam", "storage", "all")]
    [string]$Module = "all",
    
    [Parameter(Mandatory = $false)]
    [switch]$AutoApprove = $false
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Modulární Azure Functions + Service Bus Deployment" -ForegroundColor Green
Write-Host "Action: $Action" -ForegroundColor Yellow
Write-Host "Module: $Module" -ForegroundColor Yellow

# Mapování modulů na Terraform resources
$ModuleTargets = @{
    "functions" = @("azurerm_service_plan.functions_plan", "azurerm_linux_function_app.main")
    "servicebus" = @("azurerm_servicebus_namespace.main", "azurerm_servicebus_queue.main", "azurerm_servicebus_topic.main", "azurerm_servicebus_subscription.main")
    "monitoring" = @("azurerm_application_insights.main")  
    "iam" = @("azurerm_role_assignment.function_servicebus_sender")
    "storage" = @("azurerm_storage_account.functions_storage")
}

# Validace modulů
function Test-ModularStructure {
    Write-Host "`n📋 Validating modular structure..." -ForegroundColor Blue
    
    $RequiredFiles = @(
        "main.tf", "variables.tf", "outputs.tf",
        "resource-group.tf", "storage.tf", "functions.tf", 
        "servicebus.tf", "monitoring.tf", "iam.tf"
    )
    
    foreach ($file in $RequiredFiles) {
        if (!(Test-Path $file)) {
            throw "Required file missing: $file"
        }
        Write-Host "✅ Found: $file" -ForegroundColor Green
    }
    
    Write-Host "✅ All required modules found" -ForegroundColor Green
}

# Terraform operace s cílenými moduly
function Invoke-ModularTerraform {
    param($Operation, $TargetModule)
    
    $targets = @()
    
    if ($TargetModule -ne "all" -and $ModuleTargets.ContainsKey($TargetModule)) {
        $targets = $ModuleTargets[$TargetModule] | ForEach-Object { "-target=$_" }
        Write-Host "`n🎯 Targeting module: $TargetModule" -ForegroundColor Cyan
        Write-Host "Targets: $($ModuleTargets[$TargetModule] -join ', ')" -ForegroundColor Gray
    }
    
    switch ($Operation) {
        "plan" {
            if ($targets.Count -gt 0) {
                terraform plan -var-file="terraform.tfvars" @targets -out="tfplan"
            } else {
                terraform plan -var-file="terraform.tfvars" -out="tfplan"
            }
        }
        "apply" {
            if ($AutoApprove) {
                if ($targets.Count -gt 0) {
                    terraform apply -var-file="terraform.tfvars" @targets -auto-approve
                } else {
                    terraform apply -var-file="terraform.tfvars" -auto-approve  
                }
            } else {
                terraform apply "tfplan"
            }
        }
        "destroy" {
            if ($AutoApprove) {
                if ($targets.Count -gt 0) {
                    terraform destroy -var-file="terraform.tfvars" @targets -auto-approve
                } else {
                    terraform destroy -var-file="terraform.tfvars" -auto-approve
                }
            } else {
                if ($targets.Count -gt 0) {
                    terraform destroy -var-file="terraform.tfvars" @targets
                } else {
                    terraform destroy -var-file="terraform.tfvars"
                }
            }
        }
    }
    
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform $Operation failed"
    }
}

# Zobrazení modulárních výstupů
function Show-ModularOutputs {
    Write-Host "`n📊 Deployment outputs:" -ForegroundColor Blue
    terraform output
    
    Write-Host "`n🎉 Modular deployment completed!" -ForegroundColor Green
    Write-Host "Architecture:" -ForegroundColor Yellow
    Write-Host "├── Resource Group (resource-group.tf)" -ForegroundColor Gray
    Write-Host "├── Storage Account (storage.tf)" -ForegroundColor Gray  
    Write-Host "├── Function App (.NET 9) (functions.tf)" -ForegroundColor Gray
    Write-Host "├── Service Bus (servicebus.tf)" -ForegroundColor Gray
    Write-Host "├── Application Insights (monitoring.tf)" -ForegroundColor Gray
    Write-Host "└── RBAC Permissions (iam.tf)" -ForegroundColor Gray
}

# Hlavní logika
try {
    # Ověření prerekvizit
    if (!(Get-Command terraform -ErrorAction SilentlyContinue)) {
        throw "Terraform is not installed"
    }
    
    if (!(Test-Path "terraform.tfvars")) {
        if (Test-Path "terraform.tfvars.example") {
            Copy-Item "terraform.tfvars.example" "terraform.tfvars"
            throw "Created terraform.tfvars from example. Please edit it first."
        } else {
            throw "terraform.tfvars not found"
        }
    }
    
    # Validace modulární struktury
    if ($Action -eq "validate-modules") {
        Test-ModularStructure
        terraform validate
        terraform fmt -check -recursive
        Write-Host "`n✅ All modules validated successfully!" -ForegroundColor Green
        return
    }
    
    Test-ModularStructure
    
    # Terraform init
    terraform init
    
    # Provedení akce
    switch ($Action) {
        "plan" {
            Invoke-ModularTerraform "plan" $Module
        }
        "apply" {
            Invoke-ModularTerraform "plan" $Module
            Invoke-ModularTerraform "apply" $Module
            Show-ModularOutputs
        }
        "destroy" {
            Invoke-ModularTerraform "destroy" $Module
        }
    }
    
    Write-Host "`n✅ Modular operation completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "`n❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}