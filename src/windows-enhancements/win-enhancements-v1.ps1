<#
    Script Name     : win-enhancements-v1.ps1
    Description     : Windows Enhancements submenu for Technification Toolbox
    Author          : Dean John Weiniger
    Version         : 1.4
    Type            : PowerShell 7
    Date            : 2026-03-14
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path (Split-Path $PSScriptRoot -Parent) 'shared\menu-core.ps1')

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
        Write-Warn 'This submenu should be run as Administrator for best results.'
    }
}

function Invoke-EnhancementScript {
    param(
        [Parameter(Mandatory)][string]$ScriptName,
        [Parameter(Mandatory)][string]$Label
    )

    $scriptPath = Join-Path $PSScriptRoot $ScriptName
    if (-not (Test-Path -Path $scriptPath)) {
        Write-Bad "Enhancement script not found: $scriptPath"
        return
    }

    Write-Info "Running $Label..."
    & $scriptPath
    Write-Good "$Label completed."
}

function Install-OpenPowerShellAdminHere {
    Invoke-EnhancementScript -ScriptName 'open-ps-admin-here-v1.ps1' -Label 'Open PowerShell Here (Admin) context menu installer'
}

function Manage-Hibernation {
    Invoke-EnhancementScript -ScriptName 'check-hibernation-v1.ps1' -Label 'Hibernation manager'
}

function Start-DiskCleanupTool {
    Write-Info 'Launching safe disk cleanup tool...'
    Invoke-EnhancementScript -ScriptName 'disk-clean-up-v2.ps1' -Label 'Safe disk cleanup tool'
}

function Show-About {
    Write-Host ''
    Write-Host 'Windows Enhancements contains small system tweaks and convenience actions.' -ForegroundColor Cyan
    Write-Host 'Current tools: Explorer context-menu installer, hibernation manager, and safe disk cleanup.'
    Write-Host ''
    Pause
}

function Show-EnhancementsPage {
    $header = @(
        '============================================================',
        '=================== WINDOWS ENHANCEMENTS ===================',
        '============================================================'
    )
    Show-MenuPage -Title 'Enhancements Menu' -Items (& $script:GetEnhancementItems) -HeaderLines $header
}

$script:GetEnhancementItems = {
    @(
        (New-MenuItem -Key '1' -Label 'Install "Open PowerShell Here (Admin)" Context Menu' -Action { Install-OpenPowerShellAdminHere } -PauseAfter $true)
        (New-MenuItem -Key '2' -Label 'Check / Enable / Disable Hibernation' -Action { Manage-Hibernation } -PauseAfter $true)
        (New-MenuItem -Key '3' -Label 'Run Disk Cleanup Tool (Safe v2)' -Action { Start-DiskCleanupTool } -PauseAfter $true -Color 'DarkYellow')
        (New-MenuItem -Key '9' -Label 'About This Menu' -Action { Show-About })
        (New-MenuItem -Key '0' -Label 'Return To Toolbox' -Action { Write-Host 'Returning to Technification Toolbox...' -ForegroundColor Blue; return '__EXIT_MENU__' } -Color 'Blue')
    )
}

Assert-Administrator
Invoke-MenuLoop -Render { Show-EnhancementsPage } -GetItems $script:GetEnhancementItems

