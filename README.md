# Technification PowerShell Toolkit

![PowerShell 7](https://img.shields.io/badge/Code-PowerShell-012456?logo=powershell "100% PowerShell")
![Windows OS](https://img.shields.io/badge/OS-Windows-0079d4?logo=windows "Runs on Windows")
![Intel](https://img.shields.io/badge/CPU-Intel-31c5f3?logo=intel "Intel Compatible")
![AMD](https://img.shields.io/badge/CPU-AMD-00a774?logo=amd "AMD Compatible")
![Made in Australia](https://img.shields.io/badge/Made%20In-Australia-blue?logo=australia "Made in Australia")
![Difficulty: Beginner](https://img.shields.io/badge/Difficulty-Beginners-1f883d?logo=beginners "Difficulty: Beginner")
![Version 1.0](https://img.shields.io/badge/Version-1.8-yellow?logo=version "Version 1.8")  

Technification PowerShell Toolkit is a Windows-focused collection of PowerShell utilities with a single launcher and module submenus.

## Current Modules

- `Windows Auto Repair` (`v1.2`)
- `User Profile Cleanup` (`v2.1`)
- `Event Log Cleaner` (`v2.1`)
- `Windows Enhancements` (`v1.4`)
- `Network Diagnostics Suite` (`v1.1`)

## Entry Point

Run the toolbox from the repository root:

```powershell
pwsh -File .\technification-toolbox.ps1
```

## Menu Layout

Top-level menu:

```text
[1] Windows Auto Repair
[2] User Profile Cleanup
[3] Event Log Cleaner
[4] Windows Enhancements
[5] Network Diagnostics Suite
[9] About
[0] Exit
```

## Directory Layout

```text
technification-ps-toolkit/
├── technification-toolbox.ps1           Main toolbox launcher.
├── src/
│   ├── shared/
│   │   └── menu-core.ps1                Shared menu framework.
│   ├── windows-auto-repair/
│   │   ├── win-auto-repair-v1.ps1       Windows repair submenu.
│   │   └── README.md                    Module documentation.
│   ├── user-profile-cleanup/
│   │   ├── user-profile-cleanup-v2.ps1  User profile clean-up tool.
│   │   └── README.md                    Module documentation.
│   ├── event-log-cleaner/
│   │   ├── event-log-cleaner-v2.ps1     Event log cleaner submenu.
│   │   └── README.md                    Module documentation.
│   ├── windows-enhancements/
│   │   ├── win-enhancements-v1.ps1      Enhancements submenu.
│   │   ├── open-ps-admin-here-v1.ps1    Explorer context-menu helper.
│   │   ├── hibernation-manager-v1.ps1   Hibernation control helper.
│   │   ├── disk-clean-up-v2.ps1         Safer disk clean-up workflow.
│   │   └── README.md                    Module documentation.
│   └── network-diagnostics/
│       ├── network-diagnostics-v1.ps1   Network diagnostics submenu.
│       └── README.md                    Module documentation.
└── README.md                            Project overview.
```

## Requirements

- Windows 10 or Windows 11
- PowerShell 7
- Administrator rights for repair, clean-up, registry, hibernation, and some network actions

## Notes

- Menus use a shared framework in `src\shared\menu-core.ps1`.
- Most modules can be launched directly, but the toolbox is the intended entry point.
- Several tools write logs or reports under the user profile or module-specific output paths.

## License

This project is released under CC0 1.0 Universal.
