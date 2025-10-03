#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Deployment script pro Azure Functions + Service Bus

.DESCRIPTION  
    Automatizuje deployment Terraform konfigurace

.PARAMETER Action
    plan, apply, destroy

.PARAMETER Environment
    dev, test, prod

.EXAMPLE
    .\deploy.ps1 -Action plan
    .\deploy.ps1 -Action apply -Environment prod
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("plan", "apply", "destroy")]
    [string]$Action,
    
    [Parameter(Mandatory = $false)]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory = $false)]
    [switch]$AutoApprove = $false
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Azure Functions + Service Bus Deployment" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Action: $Action" -ForegroundColor Yellow

# Ověření prerekvizit
function Test-Prerequisites {
    Write-Host "`n📋 Checking prerequisites..." -ForegroundColor Blue
    
    # Terraform
    if (!(Get-Command terraform -ErrorAction SilentlyContinue)) {
        throw "Terraform is not installed"
    }
    
    # Azure CLI
    if (!(Get-Command az -ErrorAction SilentlyContinue)) {
        throw "Azure CLI is not installed"
    }
    
    # Ověření přihlášení
    try {
        $account = az account show | ConvertFrom-Json
        Write-Host "✅ Logged into Azure as: $($account.user.name)" -ForegroundColor Green
        Write-Host "✅ Subscription: $($account.name)" -ForegroundColor Green
    }
    catch {
        throw "Not logged into Azure. Run 'az login'"
    }
}

# Terraform operace
function Invoke-TerraformOperation {
    param($Operation)
    
    Write-Host "`n🔧 Running terraform $Operation..." -ForegroundColor Blue
    
    # Ověření terraform.tfvars
    if (!(Test-Path "terraform.tfvars")) {
        if (Test-Path "terraform.tfvars.example") {
            Copy-Item "terraform.tfvars.example" "terraform.tfvars"
            Write-Host "📝 Created terraform.tfvars from example. Please edit it first." -ForegroundColor Yellow
            throw "Please edit terraform.tfvars with your values"
        }
        else {
            throw "terraform.tfvars not found and no example available"
        }
    }
    
    switch ($Operation) {
        "init" {
            terraform init
        }
        "plan" {
            terraform plan -var-file="terraform.tfvars" -out="tfplan"
        }
        "apply" {
            if ($AutoApprove) {
                terraform apply -var-file="terraform.tfvars" -auto-approve
            }
            else {
                terraform apply "tfplan"
            }
        }
        "destroy" {
            if ($AutoApprove) {
                terraform destroy -var-file="terraform.tfvars" -auto-approve
            }
            else {
                terraform destroy -var-file="terraform.tfvars"
            }
        }
    }
    
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform $Operation failed"
    }
}

# Zobrazení výstupních hodnot
function Show-Outputs {
    if ($Action -eq "apply") {
        Write-Host "`n📊 Deployment outputs:" -ForegroundColor Blue
        terraform output
        
        Write-Host "`n🎉 Deployment completed successfully!" -ForegroundColor Green
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "1. Deploy your Function code using Azure Functions Core Tools" -ForegroundColor Gray
        Write-Host "2. Test the Function App endpoints" -ForegroundColor Gray  
        Write-Host "3. Configure monitoring alerts in Application Insights" -ForegroundColor Gray
    }
}

# Hlavní logika
try {
    Test-Prerequisites
    
    # Terraform init (vždy)
    Invoke-TerraformOperation "init"
    
    # Provedení požadované akce
    switch ($Action) {
        "plan" {
            Invoke-TerraformOperation "plan"
        }
        "apply" {
            Invoke-TerraformOperation "plan"
            Invoke-TerraformOperation "apply"
            Show-Outputs
        }
        "destroy" {
            Invoke-TerraformOperation "destroy"
        }
    }
    
    Write-Host "`n✅ Operation completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "`n❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}