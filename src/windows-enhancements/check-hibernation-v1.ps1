# Check current hibernation status
Write-Host "Checking hibernation status..." -ForegroundColor Cyan

$hiberStatus = powercfg /a | Select-String "Hibernate"

if ($hiberStatus -match "not available") {
    $isEnabled = $false
    Write-Host "Hibernation is currently: DISABLED" -ForegroundColor Yellow
} else {
    $isEnabled = $true
    Write-Host "Hibernation is currently: ENABLED" -ForegroundColor Green
}

Write-Host ""
Write-Host "Choose an option:"
Write-Host "1. Enable Hibernation"
Write-Host "2. Disable Hibernation"
Write-Host "3. Exit"
Write-Host ""

$choice = Read-Host "Enter your selection (1/2/3)"

switch ($choice) {
    "1" {
        if ($isEnabled) {
            Write-Host "Hibernation is already enabled." -ForegroundColor Green
        } else {
            Write-Host "Enabling hibernation..." -ForegroundColor Cyan
            powercfg /hibernate on
            Write-Host "Hibernation has been ENABLED." -ForegroundColor Green
        }
    }
    "2" {
        if (-not $isEnabled) {
            Write-Host "Hibernation is already disabled." -ForegroundColor Yellow
        } else {
            Write-Host "Disabling hibernation..." -ForegroundColor Cyan
            powercfg /hibernate off
            Write-Host "Hibernation has been DISABLED." -ForegroundColor Yellow
        }
    }
    "3" {
        Write-Host "Exiting." -ForegroundColor Gray
    }
    default {
        Write-Host "Invalid selection." -ForegroundColor Red
    }
}