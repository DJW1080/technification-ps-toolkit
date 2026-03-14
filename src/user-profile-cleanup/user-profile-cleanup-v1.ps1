<#
    Script Name     : user-profile-cleanup-v1.ps1
    Description     : Current user profile temp cleanup tool for Technification Toolbox
    Author          : Dean John Weiniger
    Version         : 1.0
    Type            : PowerShell 7
    Date            : 2026-03-14
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($Message) { Write-Host "[INFO]  $Message" -ForegroundColor Cyan }
function Write-Good($Message) { Write-Host "[GOOD]  $Message" -ForegroundColor Green }
function Write-Bad($Message) { Write-Host "[FAIL]  $Message" -ForegroundColor Red }
function Write-Warn($Message) { Write-Host "[WARN]  $Message" -ForegroundColor Yellow }

function Format-Size {
    param([Parameter(Mandatory)][Int64]$Bytes)

    if ($Bytes -ge 1GB) { return ('{0:N2} GB' -f ($Bytes / 1GB)) }
    if ($Bytes -ge 1MB) { return ('{0:N2} MB' -f ($Bytes / 1MB)) }
    if ($Bytes -ge 1KB) { return ('{0:N2} KB' -f ($Bytes / 1KB)) }
    return ("{0} B" -f $Bytes)
}

function Get-CleanupTargets {
    $userProfile = [Environment]::GetFolderPath('UserProfile')
    return @(
        [pscustomobject]@{ Name = 'Local Temp'; Path = Join-Path $userProfile 'AppData\Local\Temp' }
        [pscustomobject]@{ Name = 'INetCache'; Path = Join-Path $userProfile 'AppData\Local\Microsoft\Windows\INetCache' }
        [pscustomobject]@{ Name = 'Explorer Cache'; Path = Join-Path $userProfile 'AppData\Local\Microsoft\Windows\Explorer' }
        [pscustomobject]@{ Name = 'Crash Dumps'; Path = Join-Path $userProfile 'AppData\Local\CrashDumps' }
    )
}

function Get-PathStats {
    param(
        [Parameter(Mandatory)][string]$Path,
        [string]$Filter = '*'
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return [pscustomobject]@{ Files = 0; Bytes = 0 }
    }

    $items = @(Get-ChildItem -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue -File | Where-Object { $_.Name -like $Filter })
    $bytes = [int64]0
    foreach ($item in $items) {
        if ($null -ne $item -and $null -ne $item.Length) {
            $bytes += [int64]$item.Length
        }
    }

    return [pscustomobject]@{
        Files = $items.Count
        Bytes = [int64]$bytes
    }
}

function Get-ProfileTempScan {
    $results = @()
    foreach ($target in (Get-CleanupTargets)) {
        $filter = if ($target.Name -eq 'Explorer Cache') { 'thumbcache_*' } else { '*' }
        $stats = Get-PathStats -Path $target.Path -Filter $filter
        $results += [pscustomobject]@{
            Name = $target.Name
            Path = $target.Path
            Files = $stats.Files
            Size = $stats.Bytes
        }
    }

    $downloadsPath = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'Downloads'
    $downloadStats = Get-PathStats -Path $downloadsPath -Filter '*.tmp'
    $results += [pscustomobject]@{
        Name = 'Downloads TMP Files'
        Path = $downloadsPath
        Files = $downloadStats.Files
        Size = $downloadStats.Bytes
    }

    return $results
}

function Show-ScanReport {
    $results = Get-ProfileTempScan
    $totalFiles = 0
    $totalBytes = [int64]0

    Write-Host ''
    Write-Host '============= CURRENT USER PROFILE TEMP SCAN =============' -ForegroundColor White
    foreach ($result in $results) {
        Write-Host $result.Name -ForegroundColor Cyan
        Write-Host ("  Path  : {0}" -f $result.Path) -ForegroundColor DarkGray
        Write-Host ("  Files : {0}" -f $result.Files) -ForegroundColor DarkGray
        Write-Host ("  Size  : {0}" -f (Format-Size -Bytes $result.Size)) -ForegroundColor DarkGray
        $totalFiles += $result.Files
        $totalBytes += $result.Size
    }

    Write-Host '----------------------------------------------------------' -ForegroundColor White
    Write-Host ("Total files : {0}" -f $totalFiles) -ForegroundColor Cyan
    Write-Host ("Total size  : {0}" -f (Format-Size -Bytes $totalBytes)) -ForegroundColor Cyan
    Write-Host ''
}

function Remove-ProfileTempFiles {
    $before = Get-ProfileTempScan

    Write-Info 'Removing current user profile temp files...'
    foreach ($target in (Get-CleanupTargets)) {
        if (-not (Test-Path -LiteralPath $target.Path)) { continue }

        if ($target.Name -eq 'Explorer Cache') {
            Get-ChildItem -LiteralPath $target.Path -Force -ErrorAction SilentlyContinue -File |
                Where-Object { $_.Name -like 'thumbcache_*' } |
                Remove-Item -Force -ErrorAction SilentlyContinue
        }
        else {
            Get-ChildItem -LiteralPath $target.Path -Recurse -Force -ErrorAction SilentlyContinue |
                Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    $downloadsPath = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'Downloads'
    if (Test-Path -LiteralPath $downloadsPath) {
        Get-ChildItem -LiteralPath $downloadsPath -Force -ErrorAction SilentlyContinue -File |
            Where-Object { $_.Name -like '*.tmp' } |
            Remove-Item -Force -ErrorAction SilentlyContinue
    }

    $after = Get-ProfileTempScan

    $totalRemovedFiles = 0
    $totalRecoveredBytes = [int64]0

    Write-Host ''
    Write-Host '================ CLEANUP SUMMARY =================' -ForegroundColor White
    for ($i = 0; $i -lt $before.Count; $i++) {
        $removedFiles = [Math]::Max(0, $before[$i].Files - $after[$i].Files)
        $recoveredBytes = [Math]::Max([int64]0, [int64]($before[$i].Size - $after[$i].Size))
        $totalRemovedFiles += $removedFiles
        $totalRecoveredBytes += $recoveredBytes

        Write-Host $before[$i].Name -ForegroundColor Cyan
        Write-Host ("  Files removed   : {0}" -f $removedFiles) -ForegroundColor DarkGray
        Write-Host ("  Space recovered : {0}" -f (Format-Size -Bytes $recoveredBytes)) -ForegroundColor DarkGray
    }

    Write-Host '-------------------------------------------------' -ForegroundColor White
    Write-Host ("Total files removed   : {0}" -f $totalRemovedFiles) -ForegroundColor Cyan
    Write-Host ("Total space recovered : {0}" -f (Format-Size -Bytes $totalRecoveredBytes)) -ForegroundColor Cyan
    Write-Host ''
    Write-Good 'Current user profile temp cleanup complete.'
}

function Show-Menu {
    Clear-Host
    Write-Host '============================================================' -ForegroundColor White
    Write-Host '================= USER PROFILE CLEANUP =====================' -ForegroundColor Blue
    Write-Host '============================================================' -ForegroundColor White
    Write-Host ' [1] - Scan Current User Profile Temp Locations' -ForegroundColor Green
    Write-Host ' [2] - Remove Current User Profile Temp Files' -ForegroundColor DarkYellow
    Write-Host ' [9] - About This Tool' -ForegroundColor Cyan
    Write-Host ' [0] - Return To Toolbox' -ForegroundColor Blue
    Write-Host ''
}

function Show-About {
    Write-Host ''
    Write-Host 'This tool scans and removes temporary files from the current user profile only.' -ForegroundColor Cyan
    Write-Host 'It targets Local Temp, INetCache, Explorer thumbcache files, CrashDumps, and *.tmp files in Downloads.'
    Write-Host ''
    Pause
}

$script:ShouldExit = $false
while (-not $script:ShouldExit) {
    Show-Menu
    $choice = Read-Host '  -----> Select an option'

    switch ($choice) {
        '1' { Show-ScanReport; Pause }
        '2' { Remove-ProfileTempFiles; Pause }
        '9' { Show-About }
        '0' {
            Write-Host 'Returning to Technification Toolbox...' -ForegroundColor Blue
            $script:ShouldExit = $true
        }
        default {
            Write-Bad 'Invalid selection.'
            Pause
        }
    }
}

