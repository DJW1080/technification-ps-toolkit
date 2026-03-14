<#
    Script Name     : technification-toolbox.ps1
    Description     : Main launcher for Technification Toolbox utilities
    Author          : Dean John Weiniger
    Version         : 1.8
    Type            : PowerShell 7
    Date            : 2026-03-14
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'src\shared\menu-core.ps1')

function Write-Info($Message) { Write-Host "[INFO]  $Message" -ForegroundColor Cyan }
function Write-Good($Message) { Write-Host "[GOOD]  $Message" -ForegroundColor Green }
function Write-Bad($Message) { Write-Host "[FAIL]  $Message" -ForegroundColor Red }
function Write-Warn($Message) { Write-Host "[WARN]  $Message" -ForegroundColor Yellow }

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$toolboxVersion = '1.8'
$toolRoot = Join-Path $PSScriptRoot 'src'
$tools = @(
    [pscustomobject]@{ Id = '1'; Name = 'Windows Auto Repair'; Version = '1.2'; Description = 'DISM, SFC, cleanup, diagnostics, and repair tasks'; Path = Join-Path $toolRoot 'windows-auto-repair\win-auto-repair-v1.ps1' }
    [pscustomobject]@{ Id = '2'; Name = 'User Profile Cleanup'; Version = '2.1'; Description = 'Deep scan and category-based cleanup for the current user profile'; Path = Join-Path $toolRoot 'user-profile-cleanup\user-profile-cleanup-v2.ps1' }
    [pscustomobject]@{ Id = '3'; Name = 'Event Log Cleaner'; Version = '2.1'; Description = 'Event log cleanup with exclusions, config, and dry-run support'; Path = Join-Path $toolRoot 'event-log-cleaner\event-log-cleaner-v2.ps1' }
    [pscustomobject]@{ Id = '4'; Name = 'Windows Enhancements'; Version = '1.4'; Description = 'Context-menu tools, hibernation controls, and safe cleanup'; Path = Join-Path $toolRoot 'windows-enhancements\win-enhancements-v1.ps1' }
    [pscustomobject]@{ Id = '5'; Name = 'Network Diagnostics Suite'; Version = '1.1'; Description = 'Connectivity checks, reports, and TCP port scanning'; Path = Join-Path $toolRoot 'network-diagnostics\network-diagnostics-v1.ps1' }
)

function Get-AdminModeLabel {
    if (Test-IsAdministrator) { return 'Administrator' }
    return 'Standard User'
}

function Show-ToolboxPage {
    $header = @(
        '============================================================',
        'Technification Toolbox',
        ('Version: {0}   Mode: {1}' -f $toolboxVersion, (Get-AdminModeLabel)),
        '============================================================'
    )

    Show-MenuPage -Title 'Main Menu' -Items (& $script:GetToolboxItems) -HeaderLines $header

    if (-not (Test-IsAdministrator)) {
        Write-Host ''
        Write-Warn 'Some tools require elevation.'
    }
}

function Start-Tool {
    param([Parameter(Mandatory)][string]$Id)

    $tool = $tools | Where-Object { $_.Id -eq $Id } | Select-Object -First 1
    if (-not $tool) {
        Write-Bad 'Invalid tool selection.'
        Pause
        return
    }

    if (-not (Test-Path -Path $tool.Path)) {
        Write-Bad "Tool file not found: $($tool.Path)"
        Pause
        return
    }

    try {
        & $tool.Path
    }
    catch {
        Write-Bad "$($tool.Name) stopped with an error: $($_.Exception.Message)"
        Pause
    }
}

function Show-About {
    Write-Host ''
    Write-Host 'Technification Toolbox' -ForegroundColor Cyan
    Write-Host ("Project Path : {0}" -f $PSScriptRoot)
    Write-Host ("PowerShell   : {0}" -f $PSVersionTable.PSVersion)
    Write-Host ("Loaded Tools : {0}" -f $tools.Count)
    Write-Host ''
    Pause
}

$script:GetToolboxItems = {
    @(
        (New-MenuItem -Key '1' -Label 'Windows Auto Repair' -Description 'v1.2  DISM, SFC, cleanup, diagnostics, and repair tasks' -Action { Start-Tool -Id '1' })
        (New-MenuItem -Key '2' -Label 'User Profile Cleanup' -Description 'v2.1  Deep scan and category-based cleanup for the current user profile' -Action { Start-Tool -Id '2' })
        (New-MenuItem -Key '3' -Label 'Event Log Cleaner' -Description 'v2.1  Event log cleanup with exclusions, config, and dry-run support' -Action { Start-Tool -Id '3' })
        (New-MenuItem -Key '4' -Label 'Windows Enhancements' -Description 'v1.4  Context-menu tools, hibernation controls, and safe cleanup' -Action { Start-Tool -Id '4' })
        (New-MenuItem -Key '5' -Label 'Network Diagnostics Suite' -Description 'v1.1  Connectivity checks, reports, and TCP port scanning' -Action { Start-Tool -Id '5' })
        (New-MenuItem -Key '9' -Label 'About' -Action { Show-About } -Color 'Cyan')
        (New-MenuItem -Key '0' -Label 'Exit' -Action { Write-Host 'Exiting Technification Toolbox...' -ForegroundColor Blue; return '__EXIT_MENU__' } -Color 'Blue')
    )
}

Invoke-MenuLoop -Render { Show-ToolboxPage } -GetItems $script:GetToolboxItems
return
