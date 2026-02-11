<#
    Script Name     : event-log-clearner-v1.ps1
    Description     : Windows Event Log Cleaner Tool
    Main functions  : Deletes Windows Log files.
    Author          : Dean John Weiniger
    Version         : 1.0
    Type            : PowerShell 7
    Date            : 2026-01-21

    Features
    - Explicit exclusions (Exact + Wildcard)
    - Pre-clear per-log report: Total / Critical(Level 1) / Error(Level 2)
    - Runtime summary: logs cleared + events removed
    - Optional -WhatIf (dry run) and -Auto (skip menu)

    Run (interactive):
          pwsh -File .\event-log-cleaner-v1.ps1

    Run (no menu):
          pwsh -File .\event-log-cleaner-v1.ps1 -Auto

    Dry run:
          pwsh -File .\event-log-cleaner-v1.ps1 -WhatIf
#>

#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [switch]$Auto,
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# =========================
# Default Exclusions
# =========================

$ExcludedExactLogs = [System.Collections.Generic.List[string]]::new()
$ExcludedWildcardLogs = [System.Collections.Generic.List[string]]::new()

# Defaults (edit in menu)
$ExcludedExactLogs.Add('Security') | Out-Null
$ExcludedWildcardLogs.Add('Microsoft-Windows-Windows Defender/*') | Out-Null
function Test-IsExcludedLog {
    param([Parameter(Mandatory)][string]$LogName)

    if ($ExcludedExactLogs.Contains($LogName)) { return $true }
    foreach ($pattern in $ExcludedWildcardLogs) {
        if ($LogName -like $pattern) { return $true }
    }
    return $false
}
function Clear-EventLogWevtutil {
    param([Parameter(Mandatory)][string]$LogName)

    $wevtutil = Join-Path $env:SystemRoot 'System32\wevtutil.exe'
    $output = & $wevtutil cl "$LogName" 2>&1
    $exit   = $LASTEXITCODE

    if ($exit -ne 0) {
        $msg = ($output | Out-String).Trim()
        if (-not $msg) { $msg = "wevtutil exit code $exit (no message)" }
        throw "wevtutil failed for '$LogName' (exit $exit): $msg"
    }
}
function Show-Exclusions {
    Write-Host ""
    Write-Host "Current exclusions" -ForegroundColor Cyan
    Write-Host "  Exact:" -ForegroundColor Cyan
    if ($ExcludedExactLogs.Count -eq 0) { Write-Host "    (none)" }
    else { $ExcludedExactLogs | ForEach-Object { Write-Host "    $_" } }

    Write-Host "  Wildcards:" -ForegroundColor Cyan
    if ($ExcludedWildcardLogs.Count -eq 0) { Write-Host "    (none)" }
    else { $ExcludedWildcardLogs | ForEach-Object { Write-Host "    $_" } }
    Write-Host ""
}
function Add-ExactExclusion {
    $name = Read-Host "Enter exact log name to exclude (e.g. Security)"
    if ([string]::IsNullOrWhiteSpace($name)) { return }
    if (-not $ExcludedExactLogs.Contains($name)) {
        $ExcludedExactLogs.Add($name) | Out-Null
        Write-Host "Added exact exclusion: $name" -ForegroundColor Green
    } else {
        Write-Host "Already excluded: $name" -ForegroundColor Yellow
    }
}
function Add-WildcardExclusion {
    $pattern = Read-Host "Enter wildcard pattern to exclude (e.g. Microsoft-Windows-Windows Defender/*)"
    if ([string]::IsNullOrWhiteSpace($pattern)) { return }
    if (-not $ExcludedWildcardLogs.Contains($pattern)) {
        $ExcludedWildcardLogs.Add($pattern) | Out-Null
        Write-Host "Added wildcard exclusion: $pattern" -ForegroundColor Green
    } else {
        Write-Host "Already excluded: $pattern" -ForegroundColor Yellow
    }
}
function Remove-ExactExclusion {
    if ($ExcludedExactLogs.Count -eq 0) { Write-Host "No exact exclusions to remove." -ForegroundColor Yellow; return }
    Show-Exclusions
    $name = Read-Host "Enter exact log name to remove from exclusions"
    if ([string]::IsNullOrWhiteSpace($name)) { return }
    if ($ExcludedExactLogs.Contains($name)) {
        [void]$ExcludedExactLogs.Remove($name)
        Write-Host "Removed exact exclusion: $name" -ForegroundColor Green
    } else {
        Write-Host "Not found in exact exclusions: $name" -ForegroundColor Yellow
    }
}
function Remove-WildcardExclusion {
    if ($ExcludedWildcardLogs.Count -eq 0) { Write-Host "No wildcard exclusions to remove." -ForegroundColor Yellow; return }
    Show-Exclusions
    $pattern = Read-Host "Enter wildcard pattern to remove from exclusions"
    if ([string]::IsNullOrWhiteSpace($pattern)) { return }
    if ($ExcludedWildcardLogs.Contains($pattern)) {
        [void]$ExcludedWildcardLogs.Remove($pattern)
        Write-Host "Removed wildcard exclusion: $pattern" -ForegroundColor Green
    } else {
        Write-Host "Not found in wildcard exclusions: $pattern" -ForegroundColor Yellow
    }
}
function Get-TargetLogs {
    Get-WinEvent -ListLog * |
        Where-Object { $_.IsEnabled -eq $true -and $_.RecordCount -gt 0 -and $_.LogName } |
        Sort-Object LogName
}
function Start-ClearLogs {
    Write-Host ""
    Write-Host "Enumerating event logs..." -ForegroundColor Cyan

    $logs = Get-TargetLogs
    if (-not $logs -or $logs.Count -eq 0) {
        Write-Host "No enabled event logs with records were found." -ForegroundColor Yellow
        return
    }

    # Runtime counters
    [int64]$TotalLogsCleared   = 0
    [int64]$TotalEventsRemoved = 0
    [int64]$TotalLogsSkipped   = 0
    [int64]$TotalLogsFailed    = 0

    Write-Host ("Found {0} enabled logs with records." -f $logs.Count) -ForegroundColor Cyan
    Write-Host ("Mode: {0}" -f ($(if ($WhatIf) { "WHATIF (dry run)" } else { "LIVE (will clear logs)" }))) -ForegroundColor Cyan
    Write-Host ""

    foreach ($log in $logs) {
        $logName = $log.LogName

        if (Test-IsExcludedLog -LogName $logName) {
            Write-Host "Skipping excluded log: $logName" -ForegroundColor Yellow
            $TotalLogsSkipped++
            continue
        }

        $logEventCount = [int64]$log.RecordCount
        $criticalCount = 0
        $errorCount    = 0

        try {
            $criticalCount = (Get-WinEvent -FilterHashtable @{ LogName = $logName; Level = 1 } -ErrorAction Stop).Count
            $errorCount    = (Get-WinEvent -FilterHashtable @{ LogName = $logName; Level = 2 } -ErrorAction Stop).Count
        }
        catch {
            # Counts may fail on some channels; proceed
            $criticalCount = -1
            $errorCount    = -1
        }

        Write-Host "------------------------------------------------------------" -ForegroundColor DarkGray
        Write-Host ("Log Name        : {0}" -f $logName)
        Write-Host ("Total Records   : {0,10}" -f $logEventCount)
        Write-Host ("Critical (L1)   : {0,10}" -f $criticalCount)
        Write-Host ("Error    (L2)   : {0,10}" -f $errorCount)
        Write-Host ("Action          : {0}" -f ($(if ($WhatIf) { "SKIP (WhatIf)" } else { "CLEAR" }))) -ForegroundColor Magenta

        if ($WhatIf) {
            Write-Host "Result          : WHATIF (not cleared)" -ForegroundColor Yellow
            Write-Host ""
            continue
        }

        try {
            Clear-EventLogWevtutil -LogName $logName
            $TotalLogsCleared++
            $TotalEventsRemoved += $logEventCount
            Write-Host "Result          : CLEARED" -ForegroundColor Green
        }
        catch {
            $TotalLogsFailed++
            Write-Host "Result          : FAILED" -ForegroundColor Red
            Write-Host ("Reason          : {0}" -f $_.Exception.Message) -ForegroundColor DarkRed
        }

        Write-Host ""
    }

    Write-Host ""
    Write-Host "================= EVENT LOG CLEANER SUMMARY ================" -ForegroundColor Cyan
    Write-Host ("Total logs cleared    : {0}" -f $TotalLogsCleared) -ForegroundColor Green
    Write-Host ("Total events removed  : {0}" -f $TotalEventsRemoved) -ForegroundColor Green
    Write-Host ("Total logs skipped    : {0}" -f $TotalLogsSkipped) -ForegroundColor Yellow
    Write-Host ("Total logs failed     : {0}" -f $TotalLogsFailed) -ForegroundColor Red
    Write-Host ("Excluded exact logs   : {0}" -f ($(if ($ExcludedExactLogs.Count) { $ExcludedExactLogs -join ', ' } else { '(none)' }))) -ForegroundColor Gray
    Write-Host ("Excluded wildcards    : {0}" -f ($(if ($ExcludedWildcardLogs.Count) { $ExcludedWildcardLogs -join ', ' } else { '(none)' }))) -ForegroundColor Gray
    Write-Host "=============--=============================================" -ForegroundColor Cyan
}
function Show-Menu {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor blue
    Write-Host "================== EVENT LOG CLEANER MENU ==================" -ForegroundColor blue
    Write-Host "==================          V1.0          ==================" -ForegroundColor blue
    Write-Host "============================================================" -ForegroundColor blue
    Write-Host "1) Show current exclusions"
    Write-Host "2) Add exact exclusion"
    Write-Host "3) Add wildcard exclusion"
    Write-Host "4) Remove exact exclusion"
    Write-Host "5) Remove wildcard exclusion"
    Write-Host "6) Start Log Cleanup"
    Write-Host "7) Toggle WhatIf (dry run)  [Currently: $WhatIf]"
    Write-Host "0) Exit"
    Write-Host "============================================================" -ForegroundColor blue
}

# =========================
# Menu flow
# =========================

if ($Auto) {
    Run-Clear-Logs
    return
}

while ($true) {
    Show-Menu
    $choice = Read-Host "Select an option"

    switch ($choice) {
        '1' { Show-Exclusions }
        '2' { Add-ExactExclusion }
        '3' { Add-WildcardExclusion }
        '4' { Remove-ExactExclusion }
        '5' { Remove-WildcardExclusion }
        '6' {
            # Last safety prompt before destructive action (unless WhatIf)
            if (-not $WhatIf) {
                Show-Exclusions
                Write-Host "Type " -NoNewline
                Write-Host "OK" -NoNewline -ForegroundColor Yellow
                Write-Host " to proceed (anything else cancels)" 
                $confirm = Read-Host
                if ($confirm -ne 'OK') {
                    Write-Host "Cancelled." -ForegroundColor Yellow
                    break
                }
            }
            Start-ClearLogs
        }
        '7' {
            if ($WhatIf) { $script:WhatIf = $false } else { $script:WhatIf = $true }
            Write-Host "WhatIf is now: $WhatIf" -ForegroundColor Cyan
        }
        '0' {
            Write-Host "Exiting script." -ForegroundColor Cyan
                   return
            default { Write-Host "Invalid option." -ForegroundColor Yellow }
        }
    }
}
