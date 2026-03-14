<#
    Script Name     : win-auto-repair.ps1
    Description     : Windows Automated Image Repair Tool
    Main functions  : Repairs Windows image health, cleans temp files, and runs support diagnostics
    Author          : Dean John Weiniger
    Version         : 1.2
    Type            : PowerShell 7
    Date            : 2026-03-14
#>

$LogRoot = Join-Path $env:LOCALAPPDATA 'Win-Auto-Repair'
if (!(Test-Path $LogRoot)) { New-Item -ItemType Directory -Path $LogRoot | Out-Null }
$SessionLog = Join-Path $LogRoot ('session-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
Start-Transcript -Path $SessionLog -Force | Out-Null

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
        Write-Warn 'This tool works best when run as Administrator. Some repairs may fail without elevation.'
    }
}

function Invoke-WithSpinner {
    param(
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][string]$Command
    )

    $ActionLog = Join-Path $LogRoot ('action-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
    Write-Info "$Description started..."
    Write-Info "Log: $ActionLog"

    $process = Start-Process powershell -ArgumentList '-NoLogo', '-Command', "$Command | Out-File -FilePath '$ActionLog' -Append -Encoding utf8" -PassThru -WindowStyle Hidden
    $frames = @('|', '/', '-', '\')
    $index = 0
    while (!$process.HasExited) {
        $frame = $frames[$index % $frames.Length]
        Write-Host -NoNewline "`r[$frame] $Description..."
        Start-Sleep -Milliseconds 120
        $index++
    }
    Write-Host "`r    " -NoNewline

    if ($process.ExitCode -eq 0) { Write-Good "$Description completed." }
    else { Write-Bad "$Description failed. Check log for details: $ActionLog" }
}

function Invoke-SystemRepairTask {
    param(
        [Parameter(Mandatory)][string]$Command,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][ref]$Results
    )

    Write-Info "Running $Label..."
    $process = Start-Process -FilePath $Command -ArgumentList $Arguments -NoNewWindow -Wait -PassThru
    if ($process.ExitCode -eq 0) {
        $status = "[OK] $Label completed successfully."
        Write-Good $status
    }
    else {
        $status = "[FAIL] $Label failed with exit code $($process.ExitCode)."
        Write-Bad $status
    }
    $Results.Value += $status
}

function Start-SFC { Write-Info 'Running System File Checker...'; sfc /scannow }
function Start-DISM-CheckHealth { Write-Info 'Running Image Check Health...'; DISM /Online /Cleanup-Image /CheckHealth }
function Start-DISM-ScanHealth { Write-Info 'Running Image Scan Health...'; DISM /Online /Cleanup-Image /ScanHealth }
function Start-DISM-RestoreHealth { Write-Info 'Running Image Restore Health...'; DISM /Online /Cleanup-Image /RestoreHealth }
function Start-ComponentCleanup { Write-Info 'Running Component Cleanup...'; DISM /Online /Cleanup-Image /StartComponentCleanup }
function Start-ComponentCleanup-ResetBase { Write-Info 'Running Component Cleanup with ResetBase...'; DISM /Online /Cleanup-Image /StartComponentCleanup /ResetBase }

function Remove-TempFiles {
    Invoke-WithSpinner -Description 'Temporary File Remover' -Command "Remove-Item '$env:TEMP\*' -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item 'C:\Windows\Temp\*' -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item 'C:\Windows\SoftwareDistribution\Download\*' -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item 'C:\Windows\Prefetch\*' -Recurse -Force -ErrorAction SilentlyContinue"
}

function Repair-Network { Invoke-WithSpinner -Description 'Network Stack Reset' -Command 'netsh winsock reset; netsh int ip reset' }
function Clear-DNSCache { Invoke-WithSpinner -Description 'DNS Cache Flush' -Command 'ipconfig /flushdns; Clear-DnsClientCache' }

function Repair-WindowsUpdate {
    Invoke-WithSpinner -Description 'Windows Update Repair' -Command "Stop-Service wuauserv -Force; Stop-Service bits -Force; Stop-Service cryptsvc -Force -ErrorAction SilentlyContinue; Remove-Item 'C:\Windows\SoftwareDistribution' -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item 'C:\Windows\System32\catroot2' -Recurse -Force -ErrorAction SilentlyContinue; Start-Service cryptsvc -ErrorAction SilentlyContinue; Start-Service wuauserv; Start-Service bits"
}

function New-RestorePoint {
    Invoke-WithSpinner -Description 'Creating New Restore Point' -Command "Enable-ComputerRestore -Drive 'C:\'; Checkpoint-Computer -Description 'WinRepairPro' -RestorePointType MODIFY_SETTINGS"
}

function Start-Diagnostics {
    Invoke-WithSpinner -Description 'System Diagnostics' -Command 'systeminfo; Get-Volume; Get-PhysicalDisk -ErrorAction SilentlyContinue; Get-EventLog -LogName System -Newest 20'
}

function Start-DiskCheckScan { Write-Info 'Running online disk scan on C: ...'; chkdsk C: /scan }

function Export-SystemHealthReport {
    $reportPath = Join-Path $LogRoot ('health-report-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.txt')
    Write-Info 'Building system health report...'
    @(
        '================ SYSTEM HEALTH REPORT ================',
        "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
        "Computer : $env:COMPUTERNAME",
        "User     : $env:USERNAME",
        ''
    ) | Set-Content -Path $reportPath
    Add-Content -Path $reportPath -Value '--- SYSTEMINFO ---'
    systeminfo | Add-Content -Path $reportPath
    Add-Content -Path $reportPath -Value "`r`n--- WINDOWS EDITION ---"
    Get-ComputerInfo | Select-Object WindowsProductName, WindowsVersion, OsHardwareAbstractionLayer, OsArchitecture | Format-List | Out-String | Add-Content -Path $reportPath
    Add-Content -Path $reportPath -Value "`r`n--- DISK STATUS ---"
    Get-Volume | Format-Table DriveLetter, FileSystemLabel, FileSystem, SizeRemaining, Size -AutoSize | Out-String | Add-Content -Path $reportPath
    Add-Content -Path $reportPath -Value "`r`n--- NETWORK CONFIG ---"
    ipconfig /all | Add-Content -Path $reportPath
    Add-Content -Path $reportPath -Value "`r`n--- RECENT HOTFIXES ---"
    Get-HotFix | Sort-Object InstalledOn -Descending | Select-Object -First 15 | Format-Table HotFixID, InstalledOn, Description -AutoSize | Out-String | Add-Content -Path $reportPath
    Add-Content -Path $reportPath -Value "`r`n--- RECENT SYSTEM EVENTS ---"
    Get-EventLog -LogName System -Newest 30 | Format-Table TimeGenerated, EntryType, Source, EventID, Message -Wrap | Out-String | Add-Content -Path $reportPath
    Write-Good "System health report exported to: $reportPath"
}

function Start-RecommendedMaintenance {
    Write-Info 'Starting recommended maintenance sequence...'
    New-RestorePoint
    Start-Win-Repair
    Remove-TempFiles
    Clear-DNSCache
    Write-Good 'Recommended maintenance sequence finished.'
}

function Start-Win-Repair {
    $summary = @()
    Invoke-SystemRepairTask -Command 'dism.exe' -Arguments @('/Online', '/Cleanup-Image', '/CheckHealth') -Label 'Image Check Health' -Results ([ref]$summary)
    Invoke-SystemRepairTask -Command 'dism.exe' -Arguments @('/Online', '/Cleanup-Image', '/ScanHealth') -Label 'Image Scan Health' -Results ([ref]$summary)
    Invoke-SystemRepairTask -Command 'dism.exe' -Arguments @('/Online', '/Cleanup-Image', '/RestoreHealth') -Label 'Image Restore Health' -Results ([ref]$summary)
    Invoke-SystemRepairTask -Command 'sfc.exe' -Arguments @('/scannow') -Label 'System File Checker' -Results ([ref]$summary)
    Invoke-SystemRepairTask -Command 'sfc.exe' -Arguments @('/scannow') -Label 'System File Checker (Second Run)' -Results ([ref]$summary)
    Invoke-SystemRepairTask -Command 'dism.exe' -Arguments @('/Online', '/Cleanup-Image', '/AnalyzeComponentStore') -Label 'Analyze Component Store' -Results ([ref]$summary)
    Invoke-SystemRepairTask -Command 'dism.exe' -Arguments @('/Online', '/Cleanup-Image', '/StartComponentCleanup') -Label 'Component Cleanup' -Results ([ref]$summary)
    Invoke-SystemRepairTask -Command 'dism.exe' -Arguments @('/Online', '/Cleanup-Image', '/StartComponentCleanup', '/ResetBase') -Label 'Component Reset Base' -Results ([ref]$summary)
    Write-Host ''
    Write-Host '========================= SUMMARY =========================' -ForegroundColor Yellow
    foreach ($line in $summary) {
        if ($line -like '[OK]*') { Write-Host $line -ForegroundColor Green }
        elseif ($line -like '[FAIL]*') { Write-Host $line -ForegroundColor Red }
        else { Write-Host $line }
    }
    Write-Host '===========================================================' -ForegroundColor Yellow
}

function Show-WindowsRepairPage {
    $header = @(
        '================================================================================================',
        ' __        ___           _                        _         _                        _   _      ',
        ' \ \      / (_)_ __   __| | _____      _____     / \  _   _| |_ ___  _ __ ___   __ _| |_(_) ___ ',
        '  \ \ /\ / /| | ''  \ / _'' |/ _ \ \ /\ / / __|   / _ \| | | | __/ _ \| ''_ '' _ \ / _'' | __| |/ __|',
        '   \ V  V / | | | | | (_| | (_) \ V  V /\__ \  / ___ \ |_| | || (_) | | | | | | (_| | |_| | (__ ',
        '    \_/\_/  |_|_| |_|\__,_|\___/ \_/\_/ |___/ /_/   \_\__,_|\__\___/|_| |_| |_|\__,_|\__|_|\___|',
        '================================================================================================',
        '=== Main Menu - recommended: [13] for a full pass, or [1] then [2] manually                 ===',
        '================================================================================================'
    )
    Show-MenuPage -Title 'Windows Auto Repair Menu' -Items (& $script:GetRepairItems) -HeaderLines $header
}

$script:GetRepairItems = {
    @(
        (New-MenuItem -Key '1' -Label 'Start Windows Full Repair' -Action { Start-Win-Repair } -PauseAfter $true)
        (New-MenuItem -Key '2' -Label 'Remove Temporary Files' -Action { Remove-TempFiles } -PauseAfter $true)
        (New-MenuItem -Key '3' -Label 'DISM CheckHealth' -Action { Start-DISM-CheckHealth } -PauseAfter $true -Color 'DarkGray')
        (New-MenuItem -Key '4' -Label 'DISM ScanHealth' -Action { Start-DISM-ScanHealth } -PauseAfter $true -Color 'DarkGray')
        (New-MenuItem -Key '5' -Label 'DISM RestoreHealth' -Action { Start-DISM-RestoreHealth } -PauseAfter $true -Color 'DarkGray')
        (New-MenuItem -Key '6' -Label 'Component Cleanup' -Action { Start-ComponentCleanup } -PauseAfter $true -Color 'DarkGray')
        (New-MenuItem -Key '7' -Label 'Component Cleanup + ResetBase' -Action { Start-ComponentCleanup-ResetBase } -PauseAfter $true -Color 'DarkGray')
        (New-MenuItem -Key '8' -Label 'Repair Network Stack' -Action { Repair-Network } -PauseAfter $true -Color 'Magenta')
        (New-MenuItem -Key '9' -Label 'Repair Windows Update' -Action { Repair-WindowsUpdate } -PauseAfter $true -Color 'Magenta')
        (New-MenuItem -Key '10' -Label 'New System Restore Point' -Action { New-RestorePoint } -PauseAfter $true -Color 'DarkYellow')
        (New-MenuItem -Key '11' -Label 'Start Diagnostics' -Action { Start-Diagnostics } -PauseAfter $true -Color 'Cyan')
        (New-MenuItem -Key '12' -Label 'Start SFC' -Action { Start-SFC } -PauseAfter $true -Color 'DarkGreen')
        (New-MenuItem -Key '13' -Label 'Recommended Maintenance Sequence' -Action { Start-RecommendedMaintenance } -PauseAfter $true)
        (New-MenuItem -Key '14' -Label 'Flush DNS Cache' -Action { Clear-DNSCache } -PauseAfter $true -Color 'Magenta')
        (New-MenuItem -Key '15' -Label 'Run CHKDSK Online Scan' -Action { Start-DiskCheckScan } -PauseAfter $true -Color 'DarkYellow')
        (New-MenuItem -Key '16' -Label 'Export System Health Report' -Action { Export-SystemHealthReport } -PauseAfter $true -Color 'Cyan')
        (New-MenuItem -Key '0' -Label 'Return To Toolbox' -Action { Write-Host 'Returning to Technification Toolbox...' -ForegroundColor Blue; return '__EXIT_MENU__' } -Color 'Blue')
    )
}

Assert-Administrator
Invoke-MenuLoop -Render { Show-WindowsRepairPage } -GetItems $script:GetRepairItems
Stop-Transcript
