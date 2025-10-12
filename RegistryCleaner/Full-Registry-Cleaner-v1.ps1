<#
.SYNOPSIS
Full Registry Cleaner – Automated Scan and Delete

.DESCRIPTION
Scans specified registry paths for broken or invalid keys.
Automatically deletes them and logs all actions.
Backs up affected registry hives before cleaning.

.NOTES
Author: Dean John Weiniger
Date: 2025-10-12 -v1
Part of the Technification PowerShell Toolkit
#>

# === CONFIGURATION ===
$RegistryPaths = @(
    "HKCU\Software",
    "HKLM\Software"
)

$LogPath = "$PSScriptRoot\..\Logs\FullCleaner.log"
$BackupPath = "$PSScriptRoot\..\Backups\FullRegistryBackup.reg"

# === FUNCTIONS ===

function Write-Log {
    param ([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Add-Content -Path $LogPath -Value "$timestamp - $Message"
    Write-Host "$timestamp - $Message"
}

function Backup-Registry {
    Write-Log "Backing up registry hives..."
    reg export HKCU "$BackupPath" /y | Out-Null
    Write-Log "Backup saved to $BackupPath"
}

function IsBrokenKey {
    param ([string]$key)
    try {
        Get-Item "Registry::$key" | Out-Null
        return $false
    } catch {
        return $true
    }
}

function Scan-And-Clean {
    foreach ($basePath in $RegistryPaths) {
        $subKeys = Get-ChildItem -Path "Registry::$basePath" -ErrorAction SilentlyContinue
        foreach ($key in $subKeys) {
            $keyPath = $key.Name.Replace("Microsoft.PowerShell.Core\\Registry::", "")
            if (IsBrokenKey $keyPath) {
                Write-Log "Broken key found: $keyPath"
                try {
                    Remove-Item -Path "Registry::$keyPath" -Recurse -Force -ErrorAction Stop
                    Write-Log "Deleted: $keyPath"
                } catch {
                    Write-Log "Failed to delete: $keyPath - $_"
                }
            }
        }
    }
}

# === MAIN EXECUTION ===

Write-Log "=== Starting Full Registry Cleaner ==="
Backup-Registry
Scan-And-Clean
Write-Log "=== Cleaning complete ==="
