# ⚡ Winget Maintenance 🧰

![PowerShell 7](https://img.shields.io/badge/Code-PowerShell-012456?logo=powershell "100% PowerShell")
![Windows OS](https://img.shields.io/badge/OS-Windows-0079d4?logo=windows "Runs on Windows")
![Intel](https://img.shields.io/badge/CPU-Intel-31c5f3?logo=intel "Intel Compatible")
![AMD](https://img.shields.io/badge/CPU-AMD-00a774?logo=amd "AMD Compatible")
![Made in Australia](https://img.shields.io/badge/Made%20In-Australia-blue?logo=australia "Made in Australia")
![Difficulty: Beginner](https://img.shields.io/badge/Difficulty-Beginners-1f883d?logo=beginners "Difficulty: Beginner")
![Version 1.0](https://img.shields.io/badge/Version-1.0-yellow?logo=version "Version 1.0")

Winget Maintenance - Maintenance automation for Winget app manager.

## 📦 Run Script Directly

```powershell
pwsh -File .\winget-maintenance-v1.ps1
```

## 📋 Current Menu

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

## 🕹️ What It Does

- Detects whether `winget` is installed before enabling package actions
- Refreshes package sources
- Lists available upgrades and exports the results to the shared reports folder
- Upgrades all packages using accepted source and package agreements
- Exports the current package inventory
- Offers an explicit-confirmation source reset action

## 👨‍💻 Requirements

- Windows 11
- PowerShell 7
- Winget installed
- Administrator
