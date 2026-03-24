# Winget Maintenance

Winget Maintenance is the package maintenance module for Technification Toolbox. It automates common Winget upkeep tasks from the toolbox menu or as a standalone script.

## Run Script Directly

```powershell
pwsh -File .\winget-maintenance-v1.ps1
```

## Current Menu

```text
[1] Recommended Maintenance Sequence
[2] Check Winget Version
[3] Refresh Winget Sources
[4] List Available Upgrades
[5] Upgrade All Packages
[6] Export Installed Package Inventory
[7] Reset Winget Sources
[9] About This Menu
[0] Return To Toolbox
```

## What It Does

- Detects whether `winget` is installed before enabling package actions
- Refreshes package sources
- Lists available upgrades and exports the results to the shared reports folder
- Upgrades all packages using accepted source and package agreements
- Exports the current package inventory
- Offers an explicit-confirmation source reset action

## Requirements

- Windows 10 or Windows 11
- PowerShell 7
- Winget / App Installer installed
- Administrator rights recommended for some actions
