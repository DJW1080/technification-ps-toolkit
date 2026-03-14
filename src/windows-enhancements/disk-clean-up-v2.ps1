<#
    Script Name     : disk-clean-up-v2.ps1
    Description     : Safer Windows disk cleanup tool for Technification Toolbox
    Author          : Dean John Weiniger
    Version         : 2.1
    Type            : PowerShell 7
    Date            : 2026-03-12
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
        throw 'This cleanup tool must be run as Administrator.'
    }
}

function Format-Size {
    param([Parameter(Mandatory)][Int64]$Bytes)

    if ($Bytes -ge 1GB) { return ('{0:N2} GB' -f ($Bytes / 1GB)) }
    if ($Bytes -ge 1MB) { return ('{0:N2} MB' -f ($Bytes / 1MB)) }
    if ($Bytes -ge 1KB) { return ('{0:N2} KB' -f ($Bytes / 1KB)) }
    return ("{0} B" -f $Bytes)
}

function Get-PathStats {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return [pscustomobject]@{ Files = 0; Bytes = 0 }
    }

    $items = @(Get-ChildItem -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue | Where-Object { -not $_.PSIsContainer })
    $bytes = ($items | Measure-Object -Property Length -Sum).Sum
    if ($null -eq $bytes) { $bytes = 0 }

    return [pscustomobject]@{
        Files = $items.Count
        Bytes = [int64]$bytes
    }
}

function Invoke-CleanupStep {
    param(
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][scriptblock]$Action,
        [string[]]$TrackedPaths = @()
    )

    Write-Info $Label

    $beforeFiles = 0
    $beforeBytes = 0
    foreach ($trackedPath in $TrackedPaths) {
        $stats = Get-PathStats -Path $trackedPath
        $beforeFiles += $stats.Files
        $beforeBytes += $stats.Bytes
    }

    $status = 'Completed'
    $message = ''

    try {
        & $Action
        Write-Good "$Label completed."
    }
    catch {
        $status = 'Warning'
        $message = $_.Exception.Message
        Write-Warn "$Label skipped or failed: $message"
    }

    $afterFiles = 0
    $afterBytes = 0
    foreach ($trackedPath in $TrackedPaths) {
        $stats = Get-PathStats -Path $trackedPath
        $afterFiles += $stats.Files
        $afterBytes += $stats.Bytes
    }

    $removedFiles = [Math]::Max(0, $beforeFiles - $afterFiles)
    $recoveredBytes = [Math]::Max([int64]0, [int64]($beforeBytes - $afterBytes))

    return [pscustomobject]@{
        Label = $Label
        Status = $status
        FilesRemoved = $removedFiles
        BytesRecovered = $recoveredBytes
        Notes = $message
    }
}

function New-SafeRestorePoint {
    $restoreEnabled = Get-ComputerRestorePoint -ErrorAction SilentlyContinue
    if ($null -eq $restoreEnabled) {
        Write-Warn 'System Restore is unavailable or disabled. Skipping restore point creation.'
        return
    }

    Checkpoint-Computer -Description "Pre-DeepCleanup $(Get-Date -Format 'yyyy-MM-dd-HHmmss')" -RestorePointType 'MODIFY_SETTINGS'
}

function Start-CleanMgrProfile {
    $cleanMgr = Get-Command cleanmgr.exe -ErrorAction SilentlyContinue
    if (-not $cleanMgr) {
        Write-Warn 'cleanmgr.exe was not found. Skipping Disk Cleanup profile run.'
        return
    }

    Write-Host 'Run cleanmgr /sageset:1 first if profile 1 is not configured.' -ForegroundColor DarkGray
    Start-Process -FilePath $cleanMgr.Source -ArgumentList '/sagerun:1' -NoNewWindow -Wait
}

function Clear-TempFolders {
    Remove-Item "$env:TEMP\*" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item 'C:\Windows\Temp\*' -Recurse -Force -ErrorAction SilentlyContinue
}

function Clear-DeliveryOptimizationCache {
    $service = Get-Service -Name dosvc -ErrorAction SilentlyContinue
    if (-not $service) {
        Write-Warn 'Delivery Optimization service not found. Skipping cache cleanup.'
        return
    }

    Stop-Service -Name dosvc -Force -ErrorAction Stop
    try {
        Remove-Item 'C:\Windows\SoftwareDistribution\DeliveryOptimization\*' -Recurse -Force -ErrorAction SilentlyContinue
    }
    finally {
        Start-Service -Name dosvc -ErrorAction SilentlyContinue
    }
}

function Clear-WindowsUpdateDownloadCache {
    $service = Get-Service -Name wuauserv -ErrorAction SilentlyContinue
    if (-not $service) {
        Write-Warn 'Windows Update service not found. Skipping update cache cleanup.'
        return
    }

    Stop-Service -Name wuauserv -Force -ErrorAction Stop
    try {
        Remove-Item 'C:\Windows\SoftwareDistribution\Download\*' -Recurse -Force -ErrorAction SilentlyContinue
    }
    finally {
        Start-Service -Name wuauserv -ErrorAction SilentlyContinue
    }
}

function Clear-RecycleBinSafe {
    Clear-RecycleBin -Force -ErrorAction SilentlyContinue
}

function Start-StorageSenseTask {
    $storageSense = Get-ScheduledTask -ErrorAction SilentlyContinue | Where-Object { $_.TaskName -like '*StartStorageSense*' } | Select-Object -First 1
    if (-not $storageSense) {
        Write-Warn 'Storage Sense task not found. Skipping.'
        return
    }

    Start-ScheduledTask -TaskName $storageSense.TaskName -TaskPath $storageSense.TaskPath
}

Assert-Administrator

Write-Host ''
Write-Host '================ SAFE DISK CLEANUP =================' -ForegroundColor White
Write-Host 'Driver-store deletion has been removed from this tool.' -ForegroundColor Yellow
Write-Host 'This version keeps cleanup focused on temp data and caches.' -ForegroundColor Yellow
Write-Host ''

$results = @()
$createRestorePoint = Read-Host 'Create a restore point first? (Y/N)'
if ($createRestorePoint -match '^(Y|y)$') {
    $results += Invoke-CleanupStep -Label 'Creating restore point' -Action { New-SafeRestorePoint }
}
else {
    Write-Warn 'Restore point skipped by user choice.'
}

$results += Invoke-CleanupStep -Label 'Running Disk Cleanup profile 1' -Action { Start-CleanMgrProfile }
$results += Invoke-CleanupStep -Label 'Clearing temp folders' -Action { Clear-TempFolders } -TrackedPaths @($env:TEMP, 'C:\Windows\Temp')
$results += Invoke-CleanupStep -Label 'Clearing Delivery Optimization cache' -Action { Clear-DeliveryOptimizationCache } -TrackedPaths @('C:\Windows\SoftwareDistribution\DeliveryOptimization')
$results += Invoke-CleanupStep -Label 'Clearing Windows Update download cache' -Action { Clear-WindowsUpdateDownloadCache } -TrackedPaths @('C:\Windows\SoftwareDistribution\Download')
$results += Invoke-CleanupStep -Label 'Emptying Recycle Bin' -Action { Clear-RecycleBinSafe }
$results += Invoke-CleanupStep -Label 'Triggering Storage Sense cleanup' -Action { Start-StorageSenseTask }

$totalFiles = 0
$totalBytes = [int64]0
foreach ($result in $results) {
    if ($null -ne $result) {
        $totalFiles += [int]$result.FilesRemoved
        $totalBytes += [int64]$result.BytesRecovered
    }
}

Write-Host ''
Write-Host '================ CLEANUP SUMMARY ===================' -ForegroundColor White
foreach ($result in $results) {
    $color = if ($result.Status -eq 'Warning') { 'Yellow' } else { 'Green' }
    Write-Host ("{0} [{1}]" -f $result.Label, $result.Status) -ForegroundColor $color
    Write-Host ("  Files removed   : {0}" -f $result.FilesRemoved) -ForegroundColor DarkGray
    Write-Host ("  Space recovered : {0}" -f (Format-Size -Bytes $result.BytesRecovered)) -ForegroundColor DarkGray
    if ($result.Notes) {
        Write-Host ("  Notes           : {0}" -f $result.Notes) -ForegroundColor DarkGray
    }
}
Write-Host '---------------------------------------------------' -ForegroundColor White
Write-Host ("Total files removed   : {0}" -f $totalFiles) -ForegroundColor Cyan
Write-Host ("Total space recovered : {0}" -f (Format-Size -Bytes ([int64]$totalBytes))) -ForegroundColor Cyan
Write-Host ''
Write-Good 'Safe disk cleanup complete.'

