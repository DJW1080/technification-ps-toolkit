<#
.SYNOPSIS
Technification Registry Cleaner – Interactive and Automated

.DESCRIPTION
Provides a menu-driven interface to scan registry paths for broken or invalid keys,
delete them automatically, and log all actions. Includes optional backup, progress
output, and a summary of results. Designed for usability, safety, and transparency.

.REQUIREMENTS
- PowerShell 7.0 or later
- Windows OS (registry provider is Windows-only)

.NOTES
Author: Dean John Weiniger
Date: 2025-10-12
Version: v2.0
Project: Technification PowerShell Toolkit
Philosophy: Usability first – intuitive design, predictable behavior, and full audit trail.
#>


# === CONFIGURATION ===
$RegistryPaths = @(
    "HKCU\Software",
    "HKLM\Software"
)

$LogPath    = "$PSScriptRoot\..\Logs\FullCleaner.log"
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
    $deleted = 0
    $skipped = 0
    $scanned = 0

    foreach ($basePath in $RegistryPaths) {
        Write-Log "Scanning $basePath ..."
        $subKeys = Get-ChildItem -Path "Registry::$basePath" -ErrorAction SilentlyContinue
        foreach ($key in $subKeys) {
            $scanned++
            $keyPath = $key.Name.Replace("Microsoft.PowerShell.Core\\Registry::", "")
            Write-Progress -Activity "Scanning Registry" -Status "Checking $keyPath" -PercentComplete (($scanned / ($subKeys.Count)) * 100)

            if (IsBrokenKey $keyPath) {
                Write-Log "Broken key found: $keyPath"
                try {
                    Remove-Item -Path "Registry::$keyPath" -Recurse -Force -ErrorAction Stop
                    Write-Log "Deleted: $keyPath"
                    $deleted++
                } catch {
                    Write-Log "Failed to delete: $keyPath - $_"
                    $skipped++
                }
            }
        }
    }

    Write-Log "=== Scan Complete ==="
    Write-Host "`nSummary:"
    Write-Host "  Scanned: $scanned"
    Write-Host "  Deleted: $deleted"
    Write-Host "  Skipped: $skipped"
}

function Show-Menu {
    Clear-Host
    Write-Host "=== Technification Registry Cleaner ==="
    Write-Host "1. Backup Registry"
    Write-Host "2. Scan and Clean Broken Keys"
    Write-Host "3. Run Both (Backup + Clean)"
    Write-Host "4. Quit"
    $choice = Read-Host "Select an option (1-4)"
    return $choice
}

# === MAIN LOOP ===
do {
    $choice = Show-Menu
    switch ($choice) {
        "1" { Backup-Registry }
        "2" { Scan-And-Clean }
        "3" { Backup-Registry; Scan-And-Clean }
        "4" { Write-Host "Exiting..."; break }
        default { Write-Host "Invalid choice. Try again." }
    }

    if ($choice -ne "4") {
        $again = Read-Host "`nPress Enter to return to menu or type Q to quit"
        if ($again -eq "Q") { break }
    }
} while ($true)
