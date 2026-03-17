<#
    Script Name     : check-hibernation-v1.ps1
    Description     : Check, enable, or disable Windows hibernation
    Author          : Dean John Weiniger
    Version         : 1.1
    Type            : PowerShell 7
    Date            : 2026-03-16
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($Message) { Write-Host "[INFO]  $Message" -ForegroundColor Cyan }
function Write-Good($Message) { Write-Host "[GOOD]  $Message" -ForegroundColor Green }
function Write-Bad($Message) { Write-Host "[FAIL]  $Message" -ForegroundColor Red }
function Write-Warn($Message) { Write-Host "[WARN]  $Message" -ForegroundColor Yellow }

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Assert-Administrator {
    if (-not (Test-IsAdministrator)) {
        throw 'This hibernation tool must be run as Administrator.'
    }
}

function Get-HibernationEnabled {
    try {
        $value = Get-ItemPropertyValue -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Power' -Name 'HibernateEnabled' -ErrorAction Stop
        return ([int]$value -eq 1)
    }
    catch {
        throw 'Unable to read hibernation status from the system.'
    }
}

function Show-HibernationStatus {
    $isEnabled = Get-HibernationEnabled
    if ($isEnabled) {
        Write-Host 'Hibernation is currently: ENABLED' -ForegroundColor Green
    }
    else {
        Write-Host 'Hibernation is currently: DISABLED' -ForegroundColor Yellow
    }
    return $isEnabled
}

function Set-HibernationState {
    param([Parameter(Mandatory)][bool]$Enable)

    if ($Enable) {
        Write-Info 'Enabling hibernation...'
        powercfg /hibernate on | Out-Null
    }
    else {
        Write-Info 'Disabling hibernation...'
        powercfg /hibernate off | Out-Null
    }

    $current = Get-HibernationEnabled
    if ($current -eq $Enable) {
        if ($Enable) {
            Write-Good 'Hibernation has been ENABLED.'
        }
        else {
            Write-Good 'Hibernation has been DISABLED.'
        }
    }
    else {
        throw 'The requested hibernation change did not apply.'
    }
}

Assert-Administrator

Write-Info 'Checking hibernation status...'
$isEnabled = Show-HibernationStatus

Write-Host ''
Write-Host 'Choose an option:'
Write-Host '1. Enable Hibernation'
Write-Host '2. Disable Hibernation'
Write-Host '3. Exit'
Write-Host ''

$choice = Read-Host 'Enter your selection (1/2/3)'

switch ($choice) {
    '1' {
        if ($isEnabled) {
            Write-Good 'Hibernation is already enabled.'
        }
        else {
            Set-HibernationState -Enable $true
        }
    }
    '2' {
        if (-not $isEnabled) {
            Write-Warn 'Hibernation is already disabled.'
        }
        else {
            Set-HibernationState -Enable $false
        }
    }
    '3' {
        Write-Host 'Exiting.' -ForegroundColor Gray
    }
    default {
        Write-Bad 'Invalid selection.'
    }
}
