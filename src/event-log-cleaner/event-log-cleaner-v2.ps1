<#
Script Name     : event-log-cleaner-v2.ps1
Description     : Windows Event Log Cleaner Tool
Main functions  : Clears Windows Event Logs via wevtutil
Author          : Dean John Weiniger
Version         : 2.2
Type            : PowerShell 7
Date            : 2026-03-16
#>

#Requires -RunAsAdministrator
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [switch]$Auto,
    [switch]$DryRun,
    [bool]$SkipCounts = $true,
    [switch]$Force,
    [string]$ConfigPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path (Split-Path $PSScriptRoot -Parent) 'shared\menu-core.ps1')
. (Join-Path (Split-Path $PSScriptRoot -Parent) 'shared\logging-core.ps1')

$script:ModuleName = 'event-log-cleaner'
$script:SessionLog = New-TechnificationLogFile -ModuleName $script:ModuleName -Prefix 'session'
Write-TechnificationLog -Path $script:SessionLog -Level 'INFO' -Message 'Event Log Cleaner session started.'

$script:DefaultExcludedExact = @('Security')
$script:DefaultExcludedWildcard = @('Microsoft-Windows-Windows Defender/*')
$script:ExcludedExactLogs = [System.Collections.Generic.List[string]]::new()
$script:ExcludedWildcardLogs = [System.Collections.Generic.List[string]]::new()
$script:DefaultRemoveExact = $script:DefaultExcludedExact[0]
$script:DefaultRemoveWildcard = $script:DefaultExcludedWildcard[0]
if (-not $PSBoundParameters.ContainsKey('ConfigPath') -or [string]::IsNullOrWhiteSpace($ConfigPath)) {
    $ConfigPath = Join-Path $env:ProgramData 'Technification\EventLogCleaner\config.json'
}
if ($DryRun) { $WhatIfPreference = $true }
$script:DryRunEnabled = [bool]$WhatIfPreference

function Write-ModuleLog {
    param(
        [Parameter(Mandatory)][string]$Level,
        [Parameter(Mandatory)][string]$Message
    )

    Write-TechnificationLog -Path $script:SessionLog -Level $Level -Message $Message
}

function Initialize-ConfigStorage {
    param([Parameter(Mandatory)][string]$Path)
    $dir = Split-Path -Path $Path -Parent
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
}

function Reset-ExclusionsToDefaults {
    $script:ExcludedExactLogs.Clear()
    $script:ExcludedWildcardLogs.Clear()
    foreach ($n in $script:DefaultExcludedExact) {
        if (-not $script:ExcludedExactLogs.Contains($n)) { $script:ExcludedExactLogs.Add($n) | Out-Null }
    }
    foreach ($p in $script:DefaultExcludedWildcard) {
        if (-not $script:ExcludedWildcardLogs.Contains($p)) { $script:ExcludedWildcardLogs.Add($p) | Out-Null }
    }
}

Initialize-ConfigStorage -Path $ConfigPath
Reset-ExclusionsToDefaults

function Read-HostDefault {
    param(
        [Parameter(Mandatory)][string]$Prompt,
        [Parameter(Mandatory)][string]$Default
    )
    $value = Read-Host "$Prompt [$Default]"
    if ([string]::IsNullOrWhiteSpace($value)) { return $Default }
    return $value
}

function Test-IsExcludedLog {
    param([Parameter(Mandatory)][string]$LogName)
    if ($script:ExcludedExactLogs.Contains($LogName)) { return $true }
    foreach ($pattern in $script:ExcludedWildcardLogs) {
        if ($LogName -like $pattern) { return $true }
    }
    return $false
}

function Clear-EventLogWevtutil {
    param([Parameter(Mandatory)][string]$LogName)
    $wevtutil = Join-Path $env:SystemRoot 'System32\wevtutil.exe'
    $output = & $wevtutil cl "$LogName" 2>&1
    $exit = $LASTEXITCODE
    if ($exit -ne 0) {
        $msg = ($output | Out-String).Trim()
        if (-not $msg) { $msg = "wevtutil exit code $exit (no message)" }
        throw "wevtutil failed for '$LogName' (exit $exit): $msg"
    }
}

function Show-Exclusions {
    Write-Host ''
    Write-Host 'Current exclusions' -ForegroundColor Cyan
    Write-Host '  Exact:' -ForegroundColor Cyan
    if ($script:ExcludedExactLogs.Count -eq 0) { Write-Host '    (none)' } else { $script:ExcludedExactLogs | ForEach-Object { Write-Host "    $_" } }
    Write-Host '  Wildcards:' -ForegroundColor Cyan
    if ($script:ExcludedWildcardLogs.Count -eq 0) { Write-Host '    (none)' } else { $script:ExcludedWildcardLogs | ForEach-Object { Write-Host "    $_" } }
    Write-Host ''
    Write-Host ("Config path: {0}" -f $ConfigPath) -ForegroundColor DarkGray
    Write-Host ("SkipCounts : {0}" -f $SkipCounts) -ForegroundColor DarkGray
    Write-Host ("WhatIf     : {0}" -f $script:DryRunEnabled) -ForegroundColor DarkGray
    Write-Host ("Logs       : {0}" -f (Get-TechnificationLogsPath)) -ForegroundColor DarkGray
    Write-Host ''
    Write-ModuleLog -Level 'INFO' -Message 'Displayed exclusions/settings.'
}

function Add-ExactExclusion {
    $name = Read-Host 'Enter exact log name to exclude (e.g. Security)'
    if ([string]::IsNullOrWhiteSpace($name)) { return }
    if (-not $script:ExcludedExactLogs.Contains($name)) {
        $script:ExcludedExactLogs.Add($name) | Out-Null
        Write-Host "Added exact exclusion: $name" -ForegroundColor Green
        Write-ModuleLog -Level 'INFO' -Message ("Added exact exclusion '{0}'." -f $name)
    }
    else {
        Write-Host "Already excluded: $name" -ForegroundColor Yellow
        Write-ModuleLog -Level 'WARN' -Message ("Exact exclusion '{0}' already existed." -f $name)
    }
}

function Add-WildcardExclusion {
    $pattern = Read-Host 'Enter wildcard pattern to exclude (e.g. Microsoft-Windows-Windows Defender/*)'
    if ([string]::IsNullOrWhiteSpace($pattern)) { return }
    if (-not $script:ExcludedWildcardLogs.Contains($pattern)) {
        $script:ExcludedWildcardLogs.Add($pattern) | Out-Null
        Write-Host "Added wildcard exclusion: $pattern" -ForegroundColor Green
        Write-ModuleLog -Level 'INFO' -Message ("Added wildcard exclusion '{0}'." -f $pattern)
    }
    else {
        Write-Host "Already excluded: $pattern" -ForegroundColor Yellow
        Write-ModuleLog -Level 'WARN' -Message ("Wildcard exclusion '{0}' already existed." -f $pattern)
    }
}

function Remove-ExactExclusion {
    if ($script:ExcludedExactLogs.Count -eq 0) { Write-Host 'No exact exclusions to remove.' -ForegroundColor Yellow; return }
    Show-Exclusions
    $name = Read-HostDefault -Prompt 'Enter exact log name to remove from exclusions' -Default $script:DefaultRemoveExact
    if ([string]::IsNullOrWhiteSpace($name)) { return }
    if ($script:ExcludedExactLogs.Contains($name)) {
        [void]$script:ExcludedExactLogs.Remove($name)
        Write-Host "Removed exact exclusion: $name" -ForegroundColor Green
        Write-ModuleLog -Level 'INFO' -Message ("Removed exact exclusion '{0}'." -f $name)
    }
    else {
        Write-Host "Not found in exact exclusions: $name" -ForegroundColor Yellow
        Write-ModuleLog -Level 'WARN' -Message ("Exact exclusion '{0}' not found for removal." -f $name)
    }
}

function Remove-WildcardExclusion {
    if ($script:ExcludedWildcardLogs.Count -eq 0) { Write-Host 'No wildcard exclusions to remove.' -ForegroundColor Yellow; return }
    Show-Exclusions
    $pattern = Read-HostDefault -Prompt 'Enter wildcard pattern to remove from exclusions' -Default $script:DefaultRemoveWildcard
    if ([string]::IsNullOrWhiteSpace($pattern)) { return }
    if ($script:ExcludedWildcardLogs.Contains($pattern)) {
        [void]$script:ExcludedWildcardLogs.Remove($pattern)
        Write-Host "Removed wildcard exclusion: $pattern" -ForegroundColor Green
        Write-ModuleLog -Level 'INFO' -Message ("Removed wildcard exclusion '{0}'." -f $pattern)
    }
    else {
        Write-Host "Not found in wildcard exclusions: $pattern" -ForegroundColor Yellow
        Write-ModuleLog -Level 'WARN' -Message ("Wildcard exclusion '{0}' not found for removal." -f $pattern)
    }
}

function Save-ExclusionsConfig {
    $payload = [ordered]@{
        schemaVersion = 1
        savedAt = (Get-Date).ToString('o')
        excludedExact = @($script:ExcludedExactLogs)
        excludedWildcard = @($script:ExcludedWildcardLogs)
    }
    ($payload | ConvertTo-Json -Depth 5) | Set-Content -LiteralPath $ConfigPath -Encoding UTF8
    Write-Host "Saved exclusions to: $ConfigPath" -ForegroundColor Green
    Write-ModuleLog -Level 'INFO' -Message ("Saved exclusions config to '{0}'." -f $ConfigPath)
}

function Import-ExclusionsConfig {
    if (-not (Test-Path -LiteralPath $ConfigPath)) {
        Write-Host "Config not found: $ConfigPath" -ForegroundColor Yellow
        Write-ModuleLog -Level 'WARN' -Message ("Config file not found: '{0}'." -f $ConfigPath)
        return
    }
    try {
        $raw = Get-Content -LiteralPath $ConfigPath -Raw -Encoding UTF8
        $cfg = $raw | ConvertFrom-Json -ErrorAction Stop
        $exact = @(); $wild = @()
        if ($null -ne $cfg.excludedExact) { $exact = @($cfg.excludedExact) }
        if ($null -ne $cfg.excludedWildcard) { $wild = @($cfg.excludedWildcard) }
        $script:ExcludedExactLogs.Clear()
        $script:ExcludedWildcardLogs.Clear()
        foreach ($n in $exact) {
            if (-not [string]::IsNullOrWhiteSpace([string]$n) -and -not $script:ExcludedExactLogs.Contains([string]$n)) { $script:ExcludedExactLogs.Add([string]$n) | Out-Null }
        }
        foreach ($p in $wild) {
            if (-not [string]::IsNullOrWhiteSpace([string]$p) -and -not $script:ExcludedWildcardLogs.Contains([string]$p)) { $script:ExcludedWildcardLogs.Add([string]$p) | Out-Null }
        }
        Write-Host "Loaded exclusions from: $ConfigPath" -ForegroundColor Green
        Write-ModuleLog -Level 'INFO' -Message ("Loaded exclusions config from '{0}'." -f $ConfigPath)
    }
    catch {
        Write-Host 'Failed to load config (keeping current exclusions).' -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor DarkRed
        Write-ModuleLog -Level 'ERROR' -Message ("Failed to load config '{0}': {1}" -f $ConfigPath, $_.Exception.Message)
    }
}

if (Test-Path -LiteralPath $ConfigPath) { Import-ExclusionsConfig }

function Get-TargetLogs {
    $all = @()
    try { $all = @(Get-WinEvent -ListLog * -ErrorAction SilentlyContinue) }
    catch { $all = @() }
    $all | Where-Object { $_ -and $_.IsEnabled -eq $true -and $_.LogName -and ($_.RecordCount -as [int64]) -gt 0 } | Sort-Object LogName
}

function Get-LevelCount {
    param([Parameter(Mandatory)][string]$LogName,[Parameter(Mandatory)][int]$Level)
    try { return (Get-WinEvent -FilterHashtable @{ LogName = $LogName; Level = $Level } -ErrorAction Stop | Measure-Object | Select-Object -ExpandProperty Count) }
    catch { return -1 }
}

function Start-ClearLogs {
    [CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
    param()

    Write-Host ''
    Write-Host 'Enumerating event logs...' -ForegroundColor Cyan
    $logs = Get-TargetLogs
    if (-not $logs -or $logs.Count -eq 0) {
        Write-Host 'No enabled event logs with records were found (or access is restricted).' -ForegroundColor Yellow
        Write-ModuleLog -Level 'WARN' -Message 'No enabled event logs with records were found.'
        return
    }

    $doClear = $PSCmdlet.ShouldProcess('Enabled event logs (excluding configured exclusions)', 'Clear event logs')
    $isDryRun = $false
    if (-not $doClear) {
        if ($script:DryRunEnabled) { $isDryRun = $true }
        else {
            Write-Host 'Cancelled.' -ForegroundColor Yellow
            Write-ModuleLog -Level 'WARN' -Message 'Log clear operation cancelled before execution.'
            return
        }
    }

    [int64]$TotalLogsCleared = 0
    [int64]$TotalEventsRemoved = 0
    [int64]$TotalLogsSkipped = 0
    [int64]$TotalLogsFailed = 0

    Write-Host ("Found {0} enabled logs with records." -f $logs.Count) -ForegroundColor Cyan
    Write-Host ("Mode     : {0}" -f ($(if ($isDryRun) { 'WHATIF / DRY RUN' } else { 'LIVE (will clear logs)' }))) -ForegroundColor Cyan
    Write-Host ("Counts   : {0}" -f ($(if ($SkipCounts) { 'SKIP (fast)' } else { 'INCLUDE (slower)' }))) -ForegroundColor Cyan
    Write-Host ''

    Write-ModuleLog -Level 'INFO' -Message ("Start-ClearLogs invoked. Logs={0}; DryRun={1}; SkipCounts={2}" -f $logs.Count, $isDryRun, $SkipCounts)

    foreach ($log in $logs) {
        $logName = $log.LogName
        if (Test-IsExcludedLog -LogName $logName) {
            Write-Host "Skipping excluded log: $logName" -ForegroundColor Yellow
            $TotalLogsSkipped++
            continue
        }

        $logEventCount = [int64]($log.RecordCount -as [int64])
        $criticalCount = -1
        $errorCount = -1
        if (-not $SkipCounts) {
            $criticalCount = Get-LevelCount -LogName $logName -Level 1
            $errorCount = Get-LevelCount -LogName $logName -Level 2
        }

        Write-Host '------------------------------------------------------------' -ForegroundColor DarkGray
        Write-Host ("Log Name        : {0}" -f $logName)
        Write-Host ("Total Records   : {0,10}" -f $logEventCount)
        Write-Host ("Critical (L1)   : {0,10}" -f $criticalCount)
        Write-Host ("Error    (L2)   : {0,10}" -f $errorCount)
        Write-Host ("Action          : {0}" -f ($(if ($isDryRun) { 'SKIP (WhatIf)' } else { 'CLEAR' }))) -ForegroundColor Magenta

        if ($isDryRun) {
            Write-Host 'Result          : WHATIF (not cleared)' -ForegroundColor Yellow
            Write-Host ''
            continue
        }

        try {
            Clear-EventLogWevtutil -LogName $logName
            $TotalLogsCleared++
            $TotalEventsRemoved += $logEventCount
            Write-Host 'Result          : CLEARED' -ForegroundColor Green
        }
        catch {
            $TotalLogsFailed++
            Write-Host 'Result          : FAILED' -ForegroundColor Red
            Write-Host ("Reason          : {0}" -f $_.Exception.Message) -ForegroundColor DarkRed
        }
        Write-Host ''
    }

    Write-Host ''
    Write-Host '================= EVENT LOG CLEANER SUMMARY ================' -ForegroundColor Cyan
    Write-Host ("Total logs cleared    : {0}" -f $TotalLogsCleared) -ForegroundColor Green
    Write-Host ("Total events removed  : {0}" -f $TotalEventsRemoved) -ForegroundColor Green
    Write-Host ("Total logs skipped    : {0}" -f $TotalLogsSkipped) -ForegroundColor Yellow
    Write-Host ("Total logs failed     : {0}" -f $TotalLogsFailed) -ForegroundColor Red
    Write-Host ("Excluded exact logs   : {0}" -f ($(if ($script:ExcludedExactLogs.Count) { $script:ExcludedExactLogs -join ', ' } else { '(none)' }))) -ForegroundColor Gray
    Write-Host ("Excluded wildcards    : {0}" -f ($(if ($script:ExcludedWildcardLogs.Count) { $script:ExcludedWildcardLogs -join ', ' } else { '(none)' }))) -ForegroundColor Gray
    Write-Host '============================================================' -ForegroundColor Cyan

    Write-ModuleLog -Level 'INFO' -Message ("Log clear summary. Cleared={0}; EventsRemoved={1}; Skipped={2}; Failed={3}; DryRun={4}" -f $TotalLogsCleared, $TotalEventsRemoved, $TotalLogsSkipped, $TotalLogsFailed, $isDryRun)
}

function Show-EventLogCleanerPage {
    $header = New-MenuHeader -Name 'Event Log Cleaner' -Version '2.2' -InfoLines @(
        ("Logs    : {0}" -f (Get-TechnificationLogsPath)),
        ("Reports : {0}" -f (Get-TechnificationReportsPath))
    )
    Show-MenuPage -Title 'Cleaner Menu' -Items (& $script:GetEventLogCleanerItems) -HeaderLines $header
}

$script:GetEventLogCleanerItems = {
    @(
        (New-MenuItem -Key '1' -Label 'Show current exclusions / settings' -Action { Show-Exclusions } -PauseAfter $true)
        (New-MenuItem -Key '2' -Label 'Add exact exclusion' -Action { Add-ExactExclusion } -PauseAfter $true)
        (New-MenuItem -Key '3' -Label 'Add wildcard exclusion' -Action { Add-WildcardExclusion } -PauseAfter $true)
        (New-MenuItem -Key '4' -Label ("Remove exact exclusion (Enter = {0})" -f $script:DefaultRemoveExact) -Action { Remove-ExactExclusion } -PauseAfter $true)
        (New-MenuItem -Key '5' -Label ("Remove wildcard exclusion (Enter = {0})" -f $script:DefaultRemoveWildcard) -Action { Remove-WildcardExclusion } -PauseAfter $true)
        (New-MenuItem -Key '6' -Label 'Start Log Cleaner' -Action {
            if (-not $script:DryRunEnabled) {
                Show-Exclusions
                Write-Host 'Type ' -NoNewline
                Write-Host 'OK' -NoNewline -ForegroundColor Yellow
                Write-Host ' to proceed (anything else cancels)'
                $confirm = Read-Host
                if ($confirm -ne 'OK') {
                    Write-Host 'Cancelled.' -ForegroundColor Yellow
                    Write-ModuleLog -Level 'WARN' -Message 'Live log cleaner confirmation was cancelled by user.'
                    return
                }
            }
            Start-ClearLogs
        } -PauseAfter $true)
        (New-MenuItem -Key '7' -Label ("Toggle WhatIf (dry run) [Currently: {0}]" -f $script:DryRunEnabled) -Action {
            $script:DryRunEnabled = -not $script:DryRunEnabled
            $WhatIfPreference = $script:DryRunEnabled
            Write-Host "WhatIf is now: $WhatIfPreference" -ForegroundColor Cyan
            Write-ModuleLog -Level 'INFO' -Message ("WhatIf toggled to {0}." -f $WhatIfPreference)
        } -PauseAfter $true)
        (New-MenuItem -Key '8' -Label ("Toggle SkipCounts [Currently: {0}]" -f $SkipCounts) -Action {
            $script:SkipCounts = -not $script:SkipCounts
            Write-Host "SkipCounts is now: $SkipCounts" -ForegroundColor Cyan
            Write-ModuleLog -Level 'INFO' -Message ("SkipCounts toggled to {0}." -f $SkipCounts)
        } -PauseAfter $true)
        (New-MenuItem -Key '9' -Label 'Load exclusions from config' -Action { Import-ExclusionsConfig } -PauseAfter $true)
        (New-MenuItem -Key '10' -Label 'Save exclusions to config' -Action { Save-ExclusionsConfig } -PauseAfter $true)
        (New-MenuItem -Key '11' -Label 'Reset exclusions to defaults' -Action {
            Reset-ExclusionsToDefaults
            Write-Host 'Exclusions reset to defaults.' -ForegroundColor Green
            Write-ModuleLog -Level 'INFO' -Message 'Exclusions reset to defaults.'
        } -PauseAfter $true)
        (New-MenuItem -Key '0' -Label 'Return To Toolbox' -Action { Write-Host 'Returning to Technification Toolbox...' -ForegroundColor Cyan; return '__EXIT_MENU__' } -Color 'Blue')
    )
}

try {
    if ($Auto) {
        if (-not $Force -and -not $script:DryRunEnabled) {
            Write-ModuleLog -Level 'ERROR' -Message 'Refused LIVE -Auto without -Force.'
            throw 'Refusing LIVE -Auto without -Force. Use -Force, or use -WhatIf/-DryRun for a dry run.'
        }
        Write-ModuleLog -Level 'INFO' -Message 'Running in -Auto mode.'
        Start-ClearLogs
        return
    }

    Invoke-MenuLoop -Render { Show-EventLogCleanerPage } -GetItems $script:GetEventLogCleanerItems -Prompt 'Select an option'
}
finally {
    Write-ModuleLog -Level 'INFO' -Message 'Event Log Cleaner session ended.'
}



