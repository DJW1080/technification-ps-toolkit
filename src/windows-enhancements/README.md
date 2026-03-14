# Windows Enhancements

![PowerShell 7](https://img.shields.io/badge/Code-PowerShell-012456?logo=powershell "100% PowerShell")
![Windows OS](https://img.shields.io/badge/OS-Windows-0079d4?logo=windows "Runs on Windows")
![Intel](https://img.shields.io/badge/CPU-Intel-31c5f3?logo=intel "Intel Compatible")
![AMD](https://img.shields.io/badge/CPU-AMD-00a774?logo=amd "AMD Compatible")
![Made in Australia](https://img.shields.io/badge/Made%20In-Australia-blue?logo=australia "Made in Australia")
![Difficulty: Beginner](https://img.shields.io/badge/Difficulty-Beginners-1f883d?logo=beginners "Difficulty: Beginner")
![Version 1.0](https://img.shields.io/badge/Version-1.4-yellow?logo=version "Version 1.4")  

This module groups small Windows tweaks and helper tools under one submenu.

## Script

- `win-enhancements-v1.ps1`

## Directory Layout

```text
windows-enhancements/
├── win-enhancements-v1.ps1         Enhancements submenu.
├── open-ps-admin-here-v1.ps1       Explorer context-menu helper.
├── hibernation-manager-v1.ps1      Hibernation control helper.
├── disk-clean-up-v2.ps1            Safer disk cleanup workflow.
└── README.md                       Module documentation.
```

## Current Menu

```text
[1] Install "Open PowerShell Here (Admin)" Context Menu
[2] Check / Enable / Disable Hibernation
[3] Run Disk Cleanup Tool (Safe v2)
[9] About This Menu
[0] Return To Toolbox
```

## Included Helper Scripts

- `open-ps-admin-here-v1.ps1`
- `hibernation-manager-v1.ps1`
- `disk-clean-up-v2.ps1`

## What It Does

- Installs the Explorer context menu entry for elevated PowerShell
- Checks, enables, or disables hibernation
- Runs the safer disk cleanup workflow

## Run Directly

```powershell
pwsh -File .\src\windows-enhancements\win-enhancements-v1.ps1
```

## Requirements

- Windows 10 or Windows 11
- PowerShell 7
- Administrator rights

## Notes

- Disk cleanup v2 removed the earlier driver-store deletion logic.
- The PowerShell context-menu option uses the standalone helper script in this folder.
