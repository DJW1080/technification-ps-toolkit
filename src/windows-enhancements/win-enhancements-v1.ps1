<#
    Script Name     : win-enhancements-v1.ps1
    Description     : Windows Enhancements submenu for Technification Toolbox
    Author          : Dean John Weiniger
    Version         : 1.5
    Type            : PowerShell 7
    Date            : 2026-03-16
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path (Split-Path $PSScriptRoot -Parent) 'shared\menu-core.ps1')
. (Join-Path (Split-Path $PSScriptRoot -Parent) 'shared\logging-core.ps1')

$script:ModuleName = 'windows-enhancements'
$script:SessionLog = New-TechnificationLogFile -ModuleName $script:ModuleName -Prefix 'session'
Write-TechnificationLog -Path $script:SessionLog -Level 'INFO' -Message 'Windows Enhancements session started.'

function Write-Info($Message) { Write-Host "[INFO]  $Message" -ForegroundColor Cyan }
function Write-Good($Message) { Write-Host "[GOOD]  $Message" -ForegroundColor Green }
function Write-Bad($Message) { Write-Host "[FAIL]  $Message" -ForegroundColor Red }
function Write-Warn($Message) { Write-Host "[WARN]  $Message" -ForegroundColor Yellow }

function Write-ModuleLog {
    param(
        [Parameter(Mandatory)][string]$Level,
        [Parameter(Mandatory)][string]$Message
    )

    Write-TechnificationLog -Path $script:SessionLog -Level $Level -Message $Message
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Assert-Administrator {
    if (-not (Test-IsAdministrator)) {
        Write-Warn 'This submenu should be run as Administrator for best results.'
        Write-ModuleLog -Level 'WARN' -Message 'Session started without elevation.'
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
        Write-ModuleLog -Level 'ERROR' -Message ("Enhancement script missing: {0}" -f $scriptPath)
        return
    }

    Write-Info "Running $Label..."
    Write-ModuleLog -Level 'INFO' -Message ("Starting enhancement action '{0}' from '{1}'." -f $Label, $ScriptName)

    try {
        & $scriptPath
        Write-Good "$Label completed."
        Write-ModuleLog -Level 'INFO' -Message ("Enhancement action '{0}' completed." -f $Label)
    }
    catch {
        Write-ModuleLog -Level 'ERROR' -Message ("Enhancement action '{0}' failed: {1}" -f $Label, $_.Exception.Message)
        throw
    }
}

function Install-OpenPowerShellAdminHere {
    Invoke-EnhancementScript -ScriptName 'open-ps-admin-here-v1.ps1' -Label 'Open PowerShell Here (Admin) context menu installer'
}

function Manage-Hibernation {
    Invoke-EnhancementScript -ScriptName 'check-hibernation-v1.ps1' -Label 'Hibernation manager'
}

function Start-DiskCleanupTool {
    Write-Info 'Launching Disk cleanup tool...'
    Write-ModuleLog -Level 'INFO' -Message 'Launching Disk cleanup tool.'
    Invoke-EnhancementScript -ScriptName 'disk-clean-up-v2.ps1' -Label 'Disk cleanup tool'
}

function Show-About {
    Write-Host ''
    Write-Host 'Windows Enhancements contains small system tweaks and convenience actions.' -ForegroundColor Cyan
    Write-Host 'Current tools: Explorer context-menu installer, Hibernation manager, and Disk cleanup.'
    Write-Host ("Logs    : {0}" -f (Get-TechnificationLogsPath)) -ForegroundColor DarkGray
    Write-Host ("Reports : {0}" -f (Get-TechnificationReportsPath)) -ForegroundColor DarkGray
    Write-Host ''
    Write-ModuleLog -Level 'INFO' -Message 'About page displayed.'
    Pause
}

function Show-EnhancementsPage {
    $header = New-MenuHeader -Name 'Windows Enhancements' -Version '1.5' -InfoLines @(
        ("Logs    : {0}" -f (Get-TechnificationLogsPath)),
        ("Reports : {0}" -f (Get-TechnificationReportsPath))
    )
    Show-MenuPage -Title 'Enhancements Menu' -Items (& $script:GetEnhancementItems) -HeaderLines $header
}

$script:GetEnhancementItems = {
    @(
        (New-MenuItem -Key '1' -Label 'Install "Open PowerShell Here (Admin)" Context Menu' -Action { Install-OpenPowerShellAdminHere } -PauseAfter $true)
        (New-MenuItem -Key '2' -Label 'Enable / Disable Hibernation' -Action { Manage-Hibernation } -PauseAfter $true)
        (New-MenuItem -Key '3' -Label 'Disk Cleanup Tool' -Action { Start-DiskCleanupTool } -PauseAfter $true)
        (New-MenuItem -Key '9' -Label 'About This Menu' -Action { Show-About })
        (New-MenuItem -Key '0' -Label 'Return To Toolbox' -Action { Write-Host 'Returning to Technification Toolbox...' -ForegroundColor Blue; return '__EXIT_MENU__' } -Color 'Blue')
    )
}

Assert-Administrator

try {
    Invoke-MenuLoop -Render { Show-EnhancementsPage } -GetItems $script:GetEnhancementItems
}
finally {
    Write-ModuleLog -Level 'INFO' -Message 'Windows Enhancements session ended.'
}


