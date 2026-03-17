Set-StrictMode -Version Latest

function Get-TechnificationRootPath {
    $candidates = @()

    if (-not [string]::IsNullOrWhiteSpace($env:ProgramData)) {
        $candidates += (Join-Path $env:ProgramData 'Technification')
    }
    if (-not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
        $candidates += (Join-Path $env:LOCALAPPDATA 'Technification')
    }

    foreach ($candidate in $candidates) {
        try {
            if (-not (Test-Path -LiteralPath $candidate)) {
                New-Item -ItemType Directory -Path $candidate -Force | Out-Null
            }
            return $candidate
        }
        catch {
        }
    }

    throw 'Unable to create a writable Technification data directory.'
}

function Get-TechnificationLogsPath {
    $path = Join-Path (Get-TechnificationRootPath) 'Logs'
    if (-not (Test-Path -LiteralPath $path)) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
    }
    return $path
}

function Get-TechnificationReportsPath {
    $path = Join-Path (Get-TechnificationRootPath) 'Reports'
    if (-not (Test-Path -LiteralPath $path)) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
    }
    return $path
}

function New-TechnificationLogFile {
    param(
        [Parameter(Mandatory)][string]$ModuleName,
        [Parameter(Mandatory)][string]$Prefix
    )

    return (Join-Path (Get-TechnificationLogsPath) ('{0}-{1}-{2}.log' -f $ModuleName, $Prefix, (Get-Date -Format 'yyyyMMdd-HHmmss')))
}

function New-TechnificationReportFile {
    param(
        [Parameter(Mandatory)][string]$ModuleName,
        [Parameter(Mandatory)][string]$Prefix,
        [string]$Extension = 'txt'
    )

    return (Join-Path (Get-TechnificationReportsPath) ('{0}-{1}-{2}.{3}' -f $ModuleName, $Prefix, (Get-Date -Format 'yyyyMMdd-HHmmss'), $Extension))
}

function Write-TechnificationLog {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Level,
        [Parameter(Mandatory)][string]$Message
    )

    $line = '{0} [{1}] {2}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Level.ToUpperInvariant(), $Message
    Add-Content -Path $Path -Value $line -Encoding UTF8 -WhatIf:$false -Confirm:$false
}
